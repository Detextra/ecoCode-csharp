using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace EcoCode.Analyzers.Tests
{
    [TestClass]
    public class UseStringLengthInsteadOfEmptyStringComparisonTests
    {
        [TestMethod]
        public async Task IdentifiesEmptyStringComparison()
        {
            var testCode = @"
            class TestClass
            {
                void TestMethod()
                {
                    string s = ""Hello"";
                    if (s == """") { }  // This should be flagged
                }
            }";

            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "RCS1156",
                Message = "Replace string comparison with empty string with 'Length' check",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };

            await VerifyCSharpDiagnosticAsync(testCode, expectedDiagnostic);
        }

        [TestMethod]
        public async Task DoesNotIdentifyNonEmptyStringComparison()
        {
            var testCode = @"
            class TestClass
            {
                void TestMethod()
                {
                    string s = ""Hello"";
                    if (s == ""World"") { }  // Should not be flagged
                }
            }";

            await VerifyCSharpDiagnosticAsync(testCode);
        }

        private async Task VerifyCSharpDiagnosticAsync(string source, params DiagnosticResult[] expected)
        {
            var analyzer = new UseStringLengthInsteadOfEmptyStringComparison();
            var test = new CSharpAnalyzerTest<UseStringLengthInsteadOfEmptyStringComparison, MSTestVerifier>
            {
                TestCode = source
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }
    }
}
