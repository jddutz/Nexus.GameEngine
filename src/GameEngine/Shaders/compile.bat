@echo off
echo Compiling shaders...

for %%f in (*.vert) do (
    echo Compiling %%f...
    "C:\VulkanSDK\1.4.328.1\Bin\glslc.exe" "%%f" -o "%%~nf.vert.spv"
)

for %%f in (*.frag) do (
    echo Compiling %%f...
    "C:\VulkanSDK\1.4.328.1\Bin\glslc.exe" "%%f" -o "%%~nf.frag.spv"
)

echo Shader compilation complete!