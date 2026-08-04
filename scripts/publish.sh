#!/usr/bin/env bash
#
# Builds the Enigma.HardCopy release artifacts: one self-contained, single-file build of the desktop
# app per runtime identifier, each zipped under artifacts/.
#
# Self-contained means the .NET runtime travels inside the executable — the machine that runs the
# artifact needs nothing installed. That is the point for a tool whose whole purpose is recovering
# data years from now: the fewer moving parts between a printed sheet and the file it holds, the
# better.
#
# Usage:
#   scripts/publish.sh                    # every RID
#   scripts/publish.sh linux-x64          # just one
#   scripts/publish.sh win-x64 linux-x64  # an explicit list
#
# Requires: the .NET SDK pinned in global.json, and `zip`.

set -euo pipefail

readonly DEFAULT_RIDS=(win-x64 linux-x64)

readonly SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly ROOT="$(dirname -- "$SCRIPT_DIR")"
readonly PROJECT="$ROOT/src/Enigma.HardCopy.Desktop/Enigma.HardCopy.Desktop.csproj"
readonly ARTIFACTS="$ROOT/artifacts"

# The version lives in exactly one place — Directory.Build.props — so the artifact names cannot drift
# from what the executable reports about itself.
version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/Directory.Build.props")"
if [[ -z "$version" ]]; then
    echo "error: no <Version> found in Directory.Build.props" >&2
    exit 1
fi

rids=("$@")
if [[ ${#rids[@]} -eq 0 ]]; then
    rids=("${DEFAULT_RIDS[@]}")
fi

command -v zip >/dev/null 2>&1 || { echo "error: 'zip' is not installed" >&2; exit 1; }

echo "Enigma.HardCopy $version"
echo

for rid in "${rids[@]}"; do
    name="Enigma.HardCopy-$version-$rid"
    stage="$ARTIFACTS/publish/$name"
    archive="$ARTIFACTS/$name.zip"

    echo "==> $rid"
    rm -rf -- "$stage" "$archive"

    # GenerateDocumentationFile is off for the artifact: the XML doc file is IntelliSense material for
    # someone referencing Enigma.HardCopy.Core as a library, and means nothing to someone running the
    # app. The managed .pdb files are kept on purpose — they are what turns a crash report into a file
    # and a line number. (The far larger *native* symbol files are dropped by the csproj; see the
    # TrimNativeSymbolsFromPublish target.)
    dotnet publish "$PROJECT" \
        --configuration Release \
        --runtime "$rid" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:GenerateDocumentationFile=false \
        --output "$stage"

    # Shipped alongside the binary so an unpacked artifact is self-explanatory years later, when
    # whoever holds the printout may no longer have the repository.
    cp -- "$ROOT/README.md" "$ROOT/LICENSE.md" "$stage/"

    # Zipped from the parent so the archive expands into its own directory rather than scattering a
    # 100 MB executable across the user's Downloads folder.
    (cd -- "$ARTIFACTS/publish" && zip --quiet --recurse-paths "$archive" "$name")

    echo "    $archive ($(du -h -- "$archive" | cut -f1))"
    echo
done

echo "Done. Artifacts in $ARTIFACTS."
