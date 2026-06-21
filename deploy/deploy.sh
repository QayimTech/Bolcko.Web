#!/bin/bash
# =============================================================
#  Bolcko.Web - Linux Server Deployment Script
#  Tested on: Ubuntu 22.04 / 24.04 LTS
# =============================================================
set -e

# ─── CONFIGURABLE VARIABLES ───────────────────────────────────
APP_NAME="bolcko-web"
APP_USER="bolcko"
APP_DIR="/var/www/$APP_NAME"
SERVICE_FILE="/etc/systemd/system/$APP_NAME.service"
DOTNET_PORT=5000

DB_NAME="blockodb"
DB_USER="blockouser"
DB_PASSWORD="StrongPassword123"

# ─── COLORS ───────────────────────────────────────────────────
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()  { echo -e "${GREEN}[INFO]${NC}  $1"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# ─── ROOT CHECK ───────────────────────────────────────────────
[[ $EUID -ne 0 ]] && error "Run this script as root: sudo bash deploy.sh"

# =============================================================
#  1. SYSTEM UPDATE
# =============================================================
info "Updating system packages..."
apt-get update -y && apt-get upgrade -y

# =============================================================
#  2. INSTALL POSTGRESQL 16
# =============================================================
info "Installing PostgreSQL..."
apt-get install -y postgresql postgresql-contrib

systemctl enable postgresql
systemctl start postgresql

info "Configuring PostgreSQL database and user..."
sudo -u postgres psql <<EOF
-- Create user if not exists
DO \$\$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$DB_USER') THEN
    CREATE ROLE $DB_USER WITH LOGIN PASSWORD '$DB_PASSWORD';
  END IF;
END
\$\$;

-- Create database if not exists
SELECT 'CREATE DATABASE $DB_NAME OWNER $DB_USER'
  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DB_NAME')\gexec

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE $DB_NAME TO $DB_USER;
ALTER DATABASE $DB_NAME OWNER TO $DB_USER;
EOF

info "PostgreSQL configured: DB=$DB_NAME, User=$DB_USER"

# =============================================================
#  3. INSTALL .NET 8 RUNTIME & SDK
# =============================================================
if ! command -v dotnet &>/dev/null || ! dotnet --list-runtimes | grep -q "Microsoft.AspNetCore.App 8"; then
  info "Installing .NET 8 SDK..."
  # Microsoft package feed
  apt-get install -y wget apt-transport-https software-properties-common
  wget -q https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb \
    -O /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  rm /tmp/packages-microsoft-prod.deb
  apt-get update -y
  apt-get install -y dotnet-sdk-8.0
else
  info ".NET 8 already installed, skipping."
fi

dotnet --version
info ".NET SDK ready."

# =============================================================
#  4. INSTALL EF CORE TOOLS
# =============================================================
if ! dotnet tool list -g | grep -q "dotnet-ef"; then
  info "Installing EF Core CLI tools globally..."
  dotnet tool install --global dotnet-ef --version 8.0.11
  export PATH="$PATH:$HOME/.dotnet/tools"
  echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> /root/.bashrc
else
  info "dotnet-ef already installed."
fi

# =============================================================
#  5. CREATE APP USER & DIRECTORY
# =============================================================
if ! id "$APP_USER" &>/dev/null; then
  info "Creating system user '$APP_USER'..."
  useradd -r -s /bin/false "$APP_USER"
fi

info "Setting up application directory: $APP_DIR"
mkdir -p "$APP_DIR"
mkdir -p "$APP_DIR/logs"

# =============================================================
#  6. COPY APPLICATION FILES
# =============================================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/../publish"

if [ ! -d "$PUBLISH_DIR" ]; then
  error "Published files not found at '$PUBLISH_DIR'. Run: dotnet publish -c Release -o publish"
fi

info "Copying published files to $APP_DIR ..."
cp -r "$PUBLISH_DIR/." "$APP_DIR/"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"
chmod -R 755 "$APP_DIR"
chmod -R 775 "$APP_DIR/logs"
chmod -R 775 "$APP_DIR/wwwroot"

# =============================================================
#  7. WRITE PRODUCTION appsettings OVERRIDE (optional env file)
# =============================================================
ENV_FILE="/etc/bolcko-web.env"
info "Writing environment file: $ENV_FILE"
cat > "$ENV_FILE" <<EOF
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$DOTNET_PORT
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD
EOF
chmod 600 "$ENV_FILE"

# =============================================================
#  8. CREATE systemd SERVICE
# =============================================================
info "Creating systemd service: $SERVICE_FILE"
cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=Bolcko Web Application
After=network.target postgresql.service
Requires=postgresql.service

[Service]
Type=simple
User=$APP_USER
Group=$APP_USER
WorkingDirectory=$APP_DIR
EnvironmentFile=$ENV_FILE
ExecStart=/usr/bin/dotnet $APP_DIR/Bolcko.Web.App.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=$APP_NAME

# Security hardening
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$APP_NAME"

# =============================================================
#  9. RUN EF MIGRATIONS ON THE SERVER
# =============================================================
info "Running EF Core database migrations..."
cd "$APP_DIR"

# Set connection string for migration
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
export ASPNETCORE_ENVIRONMENT=Production

dotnet Bolcko.Web.App.dll --migrate-only 2>/dev/null || \
  dotnet ef database update \
    --connection "Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD" \
    --no-build 2>/dev/null || \
  warn "EF migration via CLI skipped - will run automatically on app start."

# =============================================================
#  10. START THE APPLICATION
# =============================================================
info "Starting $APP_NAME service..."
systemctl start "$APP_NAME"
sleep 3

if systemctl is-active --quiet "$APP_NAME"; then
  echo -e "\n${GREEN}============================================${NC}"
  echo -e "${GREEN}  ✅  Bolcko.Web deployed successfully!${NC}"
  echo -e "${GREEN}============================================${NC}"
  echo -e "  App URL:    http://$(hostname -I | awk '{print $1}'):$DOTNET_PORT"
  echo -e "  Service:    systemctl status $APP_NAME"
  echo -e "  Logs:       journalctl -u $APP_NAME -f"
  echo -e "  DB:         psql -U $DB_USER -d $DB_NAME"
  echo -e "${GREEN}============================================${NC}\n"
else
  error "Service failed to start. Check: journalctl -u $APP_NAME --no-pager -n 50"
fi
