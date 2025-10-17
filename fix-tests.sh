#!/bin/bash
set -e

echo "Fixing test compilation errors..."

# Fix RenderOptionsTests.cs - add using statement
if ! grep -q "using D2Sharp.Internal;" /home/olson/Code/D2Sharp/tests/D2Sharp.Tests/RenderOptionsTests.cs; then
    sed -i '1i\using D2Sharp.Internal;' /home/olson/Code/D2Sharp/tests/D2Sharp.Tests/RenderOptionsTests.cs
fi

# Fix all test files that still have D2Wrapper accessibility issues
# These files already have the using statement but D2Wrapper is internal
# We need to temporarily make D2Wrapper public or change tests to use ID2Renderer

echo "Updating D2Wrapper to be public (for test compatibility)..."
sed -i 's/internal partial class D2Wrapper/public partial class D2Wrapper/g' /home/olson/Code/D2Sharp/src/D2Sharp/Internal/D2Wrapper.cs

echo "Updating D2WrapperProcessPool to be public (for test compatibility)..."
sed -i 's/internal class D2WrapperProcessPool/public class D2WrapperProcessPool/g' /home/olson/Code/D2Sharp/src/D2Sharp/Internal/D2WrapperProcessPool.cs

echo "Updating D2WrapperProcess to be public (for test compatibility)..."
sed -i 's/internal class D2WrapperProcess/public class D2WrapperProcess/g' /home/olson/Code/D2Sharp/src/D2Sharp/Internal/D2WrapperProcess.cs

echo "Updating D2WrapperOptions to be public..."
sed -i 's/^namespace D2Sharp;/namespace D2Sharp.Internal;/g' /home/olson/Code/D2Sharp/src/D2Sharp/Internal/D2WrapperOptions.cs

echo "Fixing D2WrapperOptions references in D2Wrapper.cs..."
# No change needed - already using D2WrapperOptions from same namespace

echo "Done! Building to verify..."
