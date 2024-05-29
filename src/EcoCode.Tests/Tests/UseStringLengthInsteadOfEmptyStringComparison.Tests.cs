namespace EcoCode.Tests;

[TestClass]
public sealed class UseStringLengthInsteadOfEmptyStringComparisonTests
{
    private static readonly CodeFixerDlg VerifyAsync = TestRunner.VerifyAsync<UseStringLengthInsteadOfEmptyStringComparison, UseStringLengthInsteadOfEmptyStringComparisonFixer>;

    [TestMethod]
    public Task DetectsEmptyStringComparisonAsync() => VerifyAsync("""
        using System;
        public class Test
        {
            public bool IsEmpty(string s)
            {
                return s == "";
            }
        }
        """, """
        using System;
        public class Test
        {
            public bool IsEmpty(string s)
            {
                return s?.Length == 0;
            }
        }
        """);

    [TestMethod]
    public Task DetectsEmptyStringComparisonWithNotEqualsAsync() => VerifyAsync("""
        using System;
        public class Test
        {
            public bool IsNotEmpty(string s)
            {
                return s != "";
            }
        }
        """, """
        using System;
        public class Test
        {
            public bool IsNotEmpty(string s)
            {
                return s?.Length != 0;
            }
        }
        """);

    [TestMethod]
    public Task DoesNotTriggerOnNonNullCheckAsync() => VerifyAsync("""
        using System;
        public class Test
        {
            public bool HasContent(string s)
            {
                return s != null;
            }
        }
        """);

    [TestMethod]
    public Task DoesNotTriggerOnDirectLengthCheckAsync() => VerifyAsync("""
        using System;
        public class Test
        {
            public bool IsEmpty(string s)
            {
                return s.Length == 0;
            }
        }
        """);
}
