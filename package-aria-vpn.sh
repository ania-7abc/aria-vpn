#!/usr/bin/env bash
#
# Build the AriaVPN .deb package.
#
# Usage:
#   ./package-aria-vpn.sh [version] [--cores <cores-dir>] [--no-publish]
#
# Examples:
#   ./package-aria-vpn.sh 7.25.6
#   ./package-aria-vpn.sh 7.25.6 --cores /opt/AriaVPN/bin
#
# The cores directory must contain:
#   xray/xray, sing_box/sing-box, srss/*.srs, *.dat, Country.mmdb
#   (layout of the official v2rayN release archive).
#
set -euo pipefail

VERSION=""
CORES_DIR=""
DO_PUBLISH=1
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$PROJECT_DIR/v2rayN/v2rayN.Desktop/v2rayN.Desktop.csproj"
RID="linux-x64"
ARCH="amd64"
PKG_NAME="aria-vpn"
APP_NAME="AriaVPN"
PUBDIR="$PROJECT_DIR/v2rayN/v2rayN.Desktop/bin/Release/net10.0/$RID/publish"
OUTDIR="${HOME}/debbuild"

usage() {
  sed -n '2,15p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --cores)   CORES_DIR="${2:-}"; shift 2 ;;
    --no-publish) DO_PUBLISH=0; shift ;;
    -h|--help) usage; exit 0 ;;
    *)
      if [[ -z "$VERSION" ]]; then VERSION="$1"; shift; else echo "Unknown argument: $1" >&2; exit 1; fi
      ;;
  esac
done

[[ -n "$VERSION" ]] || VERSION="$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "$PROJECT_DIR/v2rayN/Directory.Build.props" 2>/dev/null | head -n1)"
[[ -n "$VERSION" ]] || VERSION="7.25.6"

if [[ -z "$CORES_DIR" ]]; then
  if [[ -d /opt/AriaVPN/bin ]]; then
    CORES_DIR="/opt/AriaVPN/bin"
  elif [[ -d /opt/v2rayN/bin ]]; then
    CORES_DIR="/opt/v2rayN/bin"
  else
    echo "Cores directory not found. Pass --cores <dir>." >&2
    exit 1
  fi
fi

echo "[*] Version:     $VERSION"
echo "[*] Cores:       $CORES_DIR"
echo "[*] Publish dir: $PUBDIR"

if [[ "$DO_PUBLISH" -eq 1 ]]; then
  echo "[*] Publishing the application (self-contained, $RID)..."
  ( cd "$PROJECT_DIR/v2rayN" && dotnet publish "$PROJECT" -c Release -r "$RID" \
      --self-contained true -p:DebugType=none -p:DebugSymbols=false )
fi

[[ -x "$PUBDIR/$APP_NAME" ]] || { echo "$PUBDIR/$APP_NAME not found - run publish first." >&2; exit 1; }

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

ROOT="$STAGE/${PKG_NAME}_${VERSION}_${ARCH}"
DEBIAN="$ROOT/DEBIAN"
APPDIR="$ROOT/opt/$APP_NAME"

mkdir -p "$APPDIR" "$DEBIAN" \
         "$ROOT/usr/bin" \
         "$ROOT/usr/share/applications" \
         "$ROOT/usr/share/icons/hicolor/256x256/apps"

echo "[*] Copying the application..."
cp -a "$PUBDIR/." "$APPDIR/"

echo "[*] Copying cores and geo files..."
mkdir -p "$APPDIR/bin/xray" "$APPDIR/bin/sing_box"
for f in xray/xray sing_box/sing-box sing_box/libcronet.so; do
  [[ -f "$CORES_DIR/$f" ]] && cp -a "$CORES_DIR/$f" "$APPDIR/bin/$f"
done
if [[ -d "$CORES_DIR/srss" ]]; then
  mkdir -p "$APPDIR/bin/srss"
  cp -a "$CORES_DIR/srss/." "$APPDIR/bin/srss/"
fi
for f in "$CORES_DIR"/*.dat "$CORES_DIR"/*.mmdb; do
  [[ -f "$f" ]] && cp -a "$f" "$APPDIR/bin/"
done
chmod 755 "$APPDIR/bin/xray/xray" "$APPDIR/bin/sing_box/sing-box" 2>/dev/null || true
chmod 755 "$APPDIR/$APP_NAME" 2>/dev/null || true

echo "[*] Installing the launcher, desktop entry and icon..."
install -m 755 /dev/stdin "$ROOT/usr/bin/aria-vpn" <<EOF
#!/usr/bin/env bash
set -euo pipefail
DIR="/opt/$APP_NAME"

# The package is installed into /opt and owned by root, so a regular user
# cannot write the configuration there. In that case keep the user data in
# ~/.local/share/$APP_NAME (the application copies the bin directory there
# on the first run).
if [[ ! -w "\$DIR" ]]; then
  DATA="\${XDG_DATA_HOME:-\$HOME/.local/share}/$APP_NAME"
  mkdir -p "\$DATA"
  export V2RAYN_LOCAL_APPLICATION_DATA_V2=1
fi

cd "\$DIR"
if [[ -x "\$DIR/$APP_NAME" ]]; then
  exec "\$DIR/$APP_NAME" "\$@"
fi
for dll in $APP_NAME.dll v2rayN.Desktop.dll v2rayN.dll; do
  if [[ -f "\$DIR/\$dll" ]]; then
    exec /usr/bin/dotnet "\$DIR/\$dll" "\$@"
  fi
done
echo "AriaVPN launcher: no executable found in \$DIR" >&2
exit 1
EOF

install -m 644 /dev/stdin "$ROOT/usr/share/applications/$PKG_NAME.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=AriaVPN
GenericName=VPN client
Comment=VPN client with automatic server selection
Exec=aria-vpn
Icon=$PKG_NAME
Terminal=false
Categories=Network;Security;
Keywords=vpn;proxy;xray;
StartupWMClass=AriaVPN
EOF

[[ -f "$PROJECT_DIR/v2rayN/v2rayN.Desktop/$APP_NAME.png" ]] \
  && install -m 644 "$PROJECT_DIR/v2rayN/v2rayN.Desktop/$APP_NAME.png" \
       "$ROOT/usr/share/icons/hicolor/256x256/apps/$PKG_NAME.png"

INSTALLED_SIZE="$(du -sk --exclude=DEBIAN "$ROOT" | cut -f1)"

cat > "$DEBIAN/control" <<EOF
Package: $PKG_NAME
Version: $VERSION
Architecture: $ARCH
Maintainer: AriaVPN <noreply@localhost>
Section: net
Priority: optional
Installed-Size: $INSTALLED_SIZE
Depends: libc6, libfontconfig1, libfreetype6, libgcc-s1, libstdc++6, fontconfig, desktop-file-utils, xdg-utils, coreutils, bash
Description: AriaVPN - VPN client with automatic server selection
 AriaVPN is based on v2rayN (Avalonia). It adds automatic server selection:
 periodic delay measurements, four selection strategies and transparent
 switching of the active server.
 Supports vless / vmess / Trojan / http / socks / Anytls / Hysteria2 /
 Shadowsocks / tuic / WireGuard.
EOF

install -m 755 /dev/stdin "$DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF

install -m 755 /dev/stdin "$DEBIAN/postrm" <<'EOF'
#!/bin/sh
set -e
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
exit 0
EOF

mkdir -p "$OUTDIR"
OUT="$OUTDIR/${PKG_NAME}_${VERSION}_${ARCH}.deb"

echo "[*] Building the package..."
dpkg-deb --root-owner-group --build "$ROOT" "$OUT" >/dev/null

echo
echo "Done: $OUT"
dpkg-deb -f "$OUT" Package Version Architecture Installed-Size
echo "Inspect: dpkg-deb -c \"$OUT\" | head"
