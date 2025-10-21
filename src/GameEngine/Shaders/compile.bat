@echo off
echo Compiling shaders to EmbeddedResources...

set OUTPUT_DIR=..\EmbeddedResources\Shaders

for %%f in (*.vert) do (
    echo Compiling %%f...
    "C:\VulkanSDK\1.4.328.1\Bin\glslc.exe" "%%f" -o "%OUTPUT_DIR%\%%~nf.vert.spv"
)

for %%f in (*.frag) do (
    echo Compiling %%f...
    "C:\VulkanSDK\1.4.328.1\Bin\glslc.exe" "%%f" -o "%OUTPUT_DIR%\%%~nf.frag.spv"
)

echo Shader compilation complete!