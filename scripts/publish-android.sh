#!/usr/bin/env bash
#
# Builds the release APK of HardCopy Recover, the Android recovery app, signed with the project's release
# key, under artifacts/.
#
# The signing key is the app's identity on every phone that installs it: Android only accepts an update
# signed by the same certificate as the app already installed. So every release is signed with the one
# release key, created once by the maintainer and kept outside the repository (docs/android.md, "Release
# APK") — never with the per-machine debug key a plain `dotnet publish` falls back to. Without the key this
# script refuses to build, so a debug-signed APK cannot be shipped by mistake.
#
# Usage:
#   export HARDCOPY_ANDROID_KEYSTORE=~/keys/hardcopy-recover.keystore
#   read -rs HARDCOPY_ANDROID_KEYSTORE_PASSWORD && export HARDCOPY_ANDROID_KEYSTORE_PASSWORD
#   scripts/publish-android.sh
#
# Environment:
#   HARDCOPY_ANDROID_KEYSTORE           the release keystore file (required)
#   HARDCOPY_ANDROID_KEYSTORE_PASSWORD  its password, which is also the key's (required)
#   HARDCOPY_ANDROID_KEY_ALIAS          the key's alias in the keystore (default: hardcopy-recover)
#
# Requires: the .NET SDK pinned in global.json, the .NET android workload, an Android SDK and a JDK.

set -euo pipefail

readonly DEFAULT_KEY_ALIAS=hardcopy-recover

readonly SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly ROOT="$(dirname -- "$SCRIPT_DIR")"
readonly PROJECT="$ROOT/src/Enigma.HardCopy.Recover.Android/Enigma.HardCopy.Recover.Android.csproj"
readonly ARTIFACTS="$ROOT/artifacts"

# The version lives in exactly one place — Directory.Build.props — so the APK's name cannot drift from the
# version it reports to Android (its versionName, and the versionCode derived from it in the csproj).
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props")"
if [[ -z "$version" ]]; then
    echo "error: no <Version> found in Directory.Build.props" >&2
    exit 1
fi

keystore="${HARDCOPY_ANDROID_KEYSTORE:-}"
key_alias="${HARDCOPY_ANDROID_KEY_ALIAS:-$DEFAULT_KEY_ALIAS}"

if [[ -z "$keystore" || -z "${HARDCOPY_ANDROID_KEYSTORE_PASSWORD:-}" ]]; then
    cat >&2 <<'EOF'
error: no release signing key is configured, so nothing was built.
Set HARDCOPY_ANDROID_KEYSTORE to the release keystore and HARDCOPY_ANDROID_KEYSTORE_PASSWORD to its
password (and HARDCOPY_ANDROID_KEY_ALIAS if the key's alias is not "hardcopy-recover"). Creating the key
is a one-time step, described in docs/android.md under "Release APK".
EOF
    exit 1
fi

if [[ ! -f "$keystore" ]]; then
    echo "error: HARDCOPY_ANDROID_KEYSTORE is '$keystore', which is not a file" >&2
    exit 1
fi

# MSBuild would resolve a relative path against the project directory, not against the caller's.
keystore="$(cd -- "$(dirname -- "$keystore")" && pwd)/$(basename -- "$keystore")"

name="Enigma.HardCopy.Recover-$version"
stage="$ARTIFACTS/publish/$name"
apk="$ARTIFACTS/$name.apk"

echo "HardCopy Recover $version"
echo

rm -rf -- "$stage" "$apk"

# The passwords go in as env: references, which the Android build resolves itself — so they appear neither
# on this command line, nor in the process list, nor in the build log.
dotnet publish "$PROJECT" \
    --configuration Release \
    --framework net10.0-android \
    -p:AndroidPackageFormat=apk \
    -p:AndroidKeyStore=true \
    -p:AndroidSigningKeyStore="$keystore" \
    -p:AndroidSigningKeyAlias="$key_alias" \
    -p:AndroidSigningStorePass=env:HARDCOPY_ANDROID_KEYSTORE_PASSWORD \
    -p:AndroidSigningKeyPass=env:HARDCOPY_ANDROID_KEYSTORE_PASSWORD \
    --output "$stage"

# The build leaves the unsigned package beside the signed one; only the signed one can be installed.
signed=("$stage"/*-Signed.apk)
if [[ ${#signed[@]} -ne 1 || ! -f "${signed[0]}" ]]; then
    echo "error: expected exactly one *-Signed.apk in $stage" >&2
    exit 1
fi

cp -- "${signed[0]}" "$apk"

echo
echo "    $apk ($(du -h -- "$apk" | cut -f1))"
echo
echo "Done. Verify the signature before publishing: apksigner verify --print-certs \"$apk\""
