#!/usr/bin/env bash
#
# CrudWithAuth Unified Deployment Coordinator
# A professional 1-command deployment wrapper for Kubernetes and VM infrastructures.
#

set -euo pipefail

# --- Color Palettes ---
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# --- Logging Helpers ---
info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}
success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}
warn() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}
error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}
header() {
    echo -e "${CYAN}${BOLD}================================================================${NC}"
    echo -e "${CYAN}${BOLD}          CrudWithAuth Deployment Orchestrator v1.0.0           ${NC}"
    echo -e "${CYAN}${BOLD}================================================================${NC}"
}

# --- Pre-flight Checks ---
check_dependencies() {
    info "Running pre-flight checks..."
    if ! command -v ansible-playbook &> /dev/null; then
        error "Ansible is not installed or not in PATH."
        echo -e "Please install Ansible on your control node first."
        echo -e "On Debian/Ubuntu: ${BOLD}sudo apt update && sudo apt install ansible -y${NC}"
        exit 1
    fi
    success "Ansible is installed."
}

# --- Deployment Runs ---
deploy_k8s() {
    header
    info "Starting Kubernetes Cluster Deployment..."
    
    PLAYBOOK="ansible/k8s_deploy.yml"
    INVENTORY="ansible/k8s_inventory.ini"
    
    if [ ! -f "$PLAYBOOK" ]; then
        error "Playbook not found: $PLAYBOOK"
        exit 1
    fi
    if [ ! -f "$INVENTORY" ]; then
        error "Inventory file not found: $INVENTORY"
        exit 1
    fi

    info "Validating playbook syntax..."
    ansible-playbook "$PLAYBOOK" -i "$INVENTORY" --syntax-check > /dev/null
    success "Syntax is valid."

    info "Executing Ansible Playbook: $PLAYBOOK..."
    echo -e "${BLUE}----------------------------------------------------------------${NC}"
    ansible-playbook "$PLAYBOOK" -i "$INVENTORY"
    echo -e "${BLUE}----------------------------------------------------------------${NC}"
    
    success "Kubernetes Stack deployed successfully!"
}

deploy_vm() {
    local inventory_file=$1
    header
    info "Starting Standard VM Stack Deployment..."
    
    PLAYBOOK="ansible/deploy.yml"
    
    if [ ! -f "$PLAYBOOK" ]; then
        error "Playbook not found: $PLAYBOOK"
        exit 1
    fi
    if [ ! -f "$inventory_file" ]; then
        error "Inventory file not found: $inventory_file"
        exit 1
    fi

    info "Validating playbook syntax..."
    ansible-playbook "$PLAYBOOK" -i "$inventory_file" --syntax-check > /dev/null
    success "Syntax is valid."

    info "Executing Ansible Playbook: $PLAYBOOK..."
    echo -e "${BLUE}----------------------------------------------------------------${NC}"
    ansible-playbook "$PLAYBOOK" -i "$inventory_file"
    echo -e "${BLUE}----------------------------------------------------------------${NC}"
    
    success "Monolithic VM Stack deployed successfully!"
}

show_help() {
    echo -e "Usage:"
    echo -e "  ${BOLD}./deploy.sh [options]${NC}"
    echo -e "\nOptions:"
    echo -e "  ${BOLD}--local${NC}                Deploy standard stack LOCALLY on this server (using localhost connection)"
    echo -e "  ${BOLD}--k8s${NC}                  Deploy to Kubernetes cluster (using k8s_inventory.ini)"
    echo -e "  ${BOLD}--vm <inventory_file>${NC}   Deploy standard stack to VMs (using specified inventory)"
    echo -e "  ${BOLD}--check${NC}               Perform syntax check on all playbooks"
    echo -e "  ${BOLD}--help${NC}                Show this help message"
    echo -e "\nIf no options are specified, an interactive menu will be shown."
}

# --- Main Logic ---
if [ $# -eq 0 ]; then
    header
    echo -e "Select your deployment target:"
    echo -e "  ${BOLD}1)${NC} Local Server Deployment (Directly on this machine)"
    echo -e "  ${BOLD}2)${NC} Kubernetes Cluster (HA Pods + Redis Cluster + Postgres)"
    echo -e "  ${BOLD}3)${NC} Monolithic VM Stack (Postgres, Redis Cluster, Nginx, Logging, Monitoring)"
    echo -e "  ${BOLD}4)${NC} Perform Syntax Check Only"
    echo -e "  ${BOLD}5)${NC} Exit"
    echo -ne "\nEnter choice [1-5]: "
    read -r choice

    case $choice in
        1)
            check_dependencies
            deploy_vm "ansible/local_inventory.ini"
            ;;
        2)
            check_dependencies
            deploy_k8s
            ;;
        3)
            echo -ne "Enter path to inventory file (e.g. ansible/k8s_inventory.ini): "
            read -r inv
            if [ -z "$inv" ]; then
                error "Inventory file cannot be empty."
                exit 1
            fi
            check_dependencies
            deploy_vm "$inv"
            ;;
        4)
            check_dependencies
            info "Checking ansible/k8s_deploy.yml..."
            ansible-playbook ansible/k8s_deploy.yml -i ansible/k8s_inventory.ini --syntax-check
            info "Checking ansible/deploy.yml..."
            ansible-playbook ansible/deploy.yml --syntax-check
            success "All playbooks syntax checks passed!"
            ;;
        5)
            info "Exiting orchestration menu."
            exit 0
            ;;
        *)
            error "Invalid choice."
            exit 1
            ;;
    esac
else
    case "$1" in
        --local)
            check_dependencies
            deploy_vm "ansible/local_inventory.ini"
            ;;
        --k8s)
            check_dependencies
            deploy_k8s
            ;;
        --vm)
            if [ -z "${2:-}" ]; then
                error "Missing inventory file for VM deployment."
                show_help
                exit 1
            fi
            check_dependencies
            deploy_vm "$2"
            ;;
        --check)
            check_dependencies
            info "Syntax check for k8s_deploy.yml..."
            ansible-playbook ansible/k8s_deploy.yml -i ansible/k8s_inventory.ini --syntax-check
            info "Syntax check for deploy.yml..."
            ansible-playbook ansible/deploy.yml --syntax-check
            success "Syntax checks passed."
            ;;
        --help|-h)
            show_help
            ;;
        *)
            error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
fi
