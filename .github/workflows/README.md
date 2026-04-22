# CI workflows

## `build.yml`

Runs on every push to `main`, every pull request, and manual dispatch.

**Matrix:** Debug + Release on `windows-latest`.

**Pipeline:**
1. Checkout, set up .NET 9 SDK, MSBuild on PATH.
2. Fetch native decoders via `build/fetch-native.ps1` (vcpkg GitHub-Actions binary cache — first run ~30-60 min, subsequent runs seconds).
3. `verify-crt.ps1` — asserts every native DLL uses consistent dynamic UCRT.
4. `dotnet restore` on Shared, Engine, Engine.Tests with `-r win-x64`.
5. `dotnet test tests/Engine.Tests/` — xUnit, TRX output.
6. `msbuild ShellExtension.vcxproj` (C++/WinRT DLL).
7. Build + run the C++ `ShellExtensionTests.exe` — exit code = failing-test count.
8. Release-only: decode signing PFX from `MSIX_TEST_CERT_PFX` secret, build MSIX via `Package.wapproj`, upload as artifact.
9. `dotnet list package --vulnerable --include-transitive` — non-blocking.

**Required secrets (Release MSIX signing):**
- `MSIX_TEST_CERT_PFX` — base64-encoded PFX file.
- `MSIX_TEST_CERT_PASSWORD` — the PFX's password.

> The Store-submission cert stays offline. Only the test cert ever lives in CI.

**Producing the base64 PFX for the secret:**
```powershell
[Convert]::ToBase64String((Get-Content -AsByteStream src/Package/RTClickPng_TemporaryKey.pfx)) | Set-Clipboard
```
Then paste into the GitHub secret value.

If the secret isn't set, CI still runs — it just skips signing and the MSIX artifact is unsigned (cannot be `Add-AppxPackage`-installed on locked-down machines but still demonstrates the build is green).

## Local parity

All the CI steps map to local commands you can run from a VS Developer PowerShell:

```powershell
./build/fetch-native.ps1                                                    # step 2
./build/verify-crt.ps1                                                      # step 3
dotnet test tests/Engine.Tests/ -c Release                                  # step 5
msbuild src/ShellExtension/ShellExtension.vcxproj /p:Configuration=Release /p:Platform=x64
./tests/ShellExtension.Tests/bin/Release/ShellExtensionTests.exe            # step 7
msbuild src/Package/Package.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Never
```
