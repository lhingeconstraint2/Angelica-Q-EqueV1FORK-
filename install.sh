#!/bin/bash
mode=${1:-install} # Allowed modes: "install" and "remove". Default: install.
service=dotnet-discord-eque # Service name
service_dir=/etc/systemd/system # Service setup folder (where all services are stored)
service_start_delay=5 # Delay in seconds before the service starts.

if [[ $(id -u) -ne 0 ]] ; then echo "Please run as root" ; exit 1 ; fi
# Check systemctl is installed
if [[ -z "$(whereis systemctl | sed 's/systemctl: //')"  ]]; then echo "systemctl is not installed"; exit 1; fi

if [[ "$mode" == "install" ]]; then
    echo "Installing service..."
    # Get current working directory of the script
    script_dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
    # Define service description
    service_description="[Unit]
Description=Dotnet Discord Eque Service
After=network.target

[Service]
Type=simple
WorkingDirectory=$script_dir/DiscordEqueBot
ExecStart=/usr/bin/dotnet run
Restart=always
RestartSec=5
StartLimitInterval=0
SyslogIdentifier=$service
User=root
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target"

    # Install service
    echo "$service_description" > "$service_dir"/"$service".service
    systemctl daemon-reload
    systemctl enable "$service"
    sleep "$service_start_delay"
    systemctl start "$service"
    echo "Service installed and started."
elif [[ "$mode" == "remove" ]]; then
    echo "Removing service..."
    # Stop service if running
    systemctl stop "$service"
    # Disable service
    systemctl disable "$service"
    # Remove service file
    rm "$service_dir"/"$service".service
    # Reload systemd after removal
    systemctl daemon-reload
    echo "Service removed."
elif [[ "$mode" == "restart" ]]; then
    echo "Restarting service..."
    systemctl restart "$service"
    echo "Service restarted."
elif [[ "$mode" == "start" ]]; then
    echo "Starting service..."
    systemctl start "$service"
    echo "Service started."
elif [[ "$mode" == "stop" ]]; then
    echo "Stopping service..."
    systemctl stop "$service"
    echo "Service stopped."
else
    echo "Invalid mode. Allowed modes are 'install', 'remove', 'restart', 'start', 'stop'."
    exit 1
fi
