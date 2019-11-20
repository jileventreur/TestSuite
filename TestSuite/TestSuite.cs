using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PhenoWareCommon
{
    /// <summary>
    /// Class representing an assert to test. The assert is passed through constructor and evaluated with Exec method 
    /// </summary>
    public class Test
    {
        /// <summary>
        /// Initialize a new instance of Test with given assert expression to test
        /// </summary>
        /// <param name="assertExpr">Assert expression to test</param>
        /// <param name="errorMessage">Message describing error if test fail</param>        
        public Test(Expression<Func<bool>> assertExpr, string errorMessage = "")
        {
            var test = new Dictionary<string, string>(4);
            AssertExpr = assertExpr;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Initialize a new instance of Test from a ValueTuple containing both assert expression to test and a message describing error if test fail
        /// </summary>
        /// <param name="vtuple"></param>
        public Test((Expression<Func<bool>> assertExpr, string errorMessage) vtuple) : this(vtuple.assertExpr, vtuple.errorMessage) { }



        /// <summary>
        /// Implicit cast operator of ValueTuple(Expression<Func<bool>>, string) into Test
        /// </summary>
        /// <param name="vtuple"></param>
        /// <remarks>
        /// Mainny defined to allow successive Test construction in TestSuite construction with a collection initializer
        /// Use ValueTuple Test constructor.
        /// </remarks>
        public static implicit operator Test((Expression<Func<bool>>, string) vtuple) => new Test(vtuple);

        /// <summary>
        /// Expression who will be evaluated during Exec. Considered as failed if the result is false
        /// </summary>
        public Expression<Func<bool>> AssertExpr { get; private set; }


        /// <summary>
        /// String describing the error if test failed 
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Tests evaluation. Compile and execute AssertExpr attribute
        /// </summary>
        /// <returns></returns>
        public bool Exec() { return AssertExpr.Compile()(); }

        ///  ---------- Test Combination Section ----------

        /// <summary>
        /// Base class defining error message combine behaviour to adopt during Test combination
        /// </summary>
        /// <remarks>
        /// Uses a enum like class associating static instances representing the designed behaviour associated with a combination function
        /// </remarks>
        public class MessageCombineMode
        {
            /// <summary>
            /// String combination function prototype
            /// </summary>
            /// <param name="oldStr">ErrorMessage from base Test (left hand side)</param>
            /// <param name="newStr">ErrorMessage from Test to combine (right hand side)</param>
            /// <param name="combineStr">String used in Concat behavior</param>
            /// <returns></returns>
            public delegate string CombineMsgFunction(string oldStr, string newStr, string combineStr);

            /// <summary>
            /// String combination function attribute
            /// </summary>
            /// <remarks>
            /// Ideally this function should not be public but I can't find a way to give access to this function to Test without using an interface
            /// Same remark for CombineMsgFunction prototype
            /// </remarks>
            public CombineMsgFunction Exec { get; set; }

            /// <summary>
            /// Private constructor used to construct 
            /// </summary>
            /// <param name="combineFunc"></param>
            private MessageCombineMode(CombineMsgFunction combineFunc) { Exec = combineFunc; }

            /// <summary>
            /// Uses ErrorMessage from right hand side Test in generated Test
            /// </summary>
            public static readonly MessageCombineMode Replace = new MessageCombineMode((oldStr, newStr, combineStr) => newStr);

            /// <summary>
            /// Uses ErrorMessage from left hand side Test in generated Test
            /// </summary>
            public static readonly MessageCombineMode RetainOriginal = new MessageCombineMode((oldStr, newStr, combineStr) => oldStr);

            /// <summary>
            /// Concat ErrorMessage from left and Right hand side Test for ErrorMEssage generated Test
            /// </summary>
            public static readonly MessageCombineMode Concat = new MessageCombineMode((oldStr, newStr, combineStr) => oldStr + " " + combineStr + " " + newStr);
        }

        /// <summary>
        /// Assert expression combination delegate prototype
        /// </summary>
        /// <param name="expr1"></param>
        /// <param name="expr2"></param>
        /// <returns></returns>
        private delegate Expression<Func<bool>> ComineDelegate(Expression<Func<bool>> expr1, Expression<Func<bool>> expr2);

        /// <summary>
        /// Generic combine Test function. Uses the given combineFunc to combine both AssertExpr and given combineMode to combine ErrorMessage
        /// </summary>
        /// <param name="right">Right hand side Test</param>
        /// <param name="combineFunc">Function used to combine both AssertExpr</param>
        /// <param name="combineMode">Class used to combine both ErrorMessage</param>
        /// <param name="combineStr">String used to describe combination type</param>
        /// <returns></returns>
        private Test combine(Test right, ComineDelegate combineFunc, MessageCombineMode combineMode, string combineStr = "")
        {
            return new Test
            (
                combineFunc(this.AssertExpr, right.AssertExpr),
                combineMode.Exec(this.ErrorMessage, right.ErrorMessage, combineStr)
            );
        }

        /// <summary>
        /// Creates a Test that evaluate a conditionnal OR operation that evaluates right test assert only if the left evaluates to false
        /// </summary>
        /// <param name="right"></param>
        /// <param name="combineMode">ErrorMessages combination mode</param>
        /// <returns></returns>
        public Test Or(Test right, MessageCombineMode combineMode)
        {
            return combine
            (
                right,
                (a, b) => Expression.Lambda<Func<bool>>(Expression.OrElse(a.Body, b.Body)),
                combineMode,
                "Or"
            );
        }

        /// <summary>
        /// Creates a test containing an assert representing a conditional OR that evaluates 
        /// the right assert only if the left assert is evaluated as false
        /// <para>By default ErrorMessage remain the same</para> 
        /// </summary>
        /// <param name="right"></param>
        /// <param name="combineMode">ErrorMessages combination mode</param>
        /// <returns></returns>
        public Test Or(Test right) { return Or(right, MessageCombineMode.Replace); }

        /// <summary>
        /// Creates a test containing an assert representing a conditional OR that evaluates
        /// the right assert only if the assert of the initial object is evaluated as false
        /// <para>By default ErrorMessage remains the same</para> 
        /// </summary>
        /// <param name="rightAssert"></param>
        /// <returns></returns>
        public Test Or(Expression<Func<bool>> rightAssert) { return Or(new Test(rightAssert, ""), MessageCombineMode.RetainOriginal); }

        /// <summary>
        /// Creates a test containing an assert representing a conditional OR that evaluates
        /// the right assert only if the assert of the initial instance is evaluated as false
        /// <para>By default ErrorMessage remains the same</para> 
        /// </summary>
        /// <param name="rightAssert"></param>
        /// <param name="ErrorMessage">Error message of generated Test object</param>
        /// <returns></returns>
        public Test Or(Expression<Func<bool>> rightAssert, string ErrorMessage) { return Or(new Test(rightAssert, ErrorMessage), MessageCombineMode.Replace); }

        /// <summary>
        /// Creates a Test that evaluate a conditionnal AND operation that evaluates right test assert only if the left evaluates to true
        /// </summary>
        /// <param name="right"></param>
        /// <param name="combineMode">ErrorMessages combination mode</param>
        /// <returns></returns>
        public Test And(Test right, MessageCombineMode combineMode)
        {
            return combine
            (
                right,
                (a, b) => Expression.Lambda<Func<bool>>(Expression.AndAlso(a.Body, b.Body)),
                combineMode,
                "And"
            );
        }

        /// <summary>
        /// Creates a test containing an assert representing a conditional AND that evaluates
        /// the right assert only if the assert of the initial object is evaluated as trues
        /// <para>By default ErrorMessage remains the same</para> 
        /// </summary>
        /// <param name="rightAssert"></param>
        /// <returns></returns>
        public Test And(Test newTest) { return And(newTest, MessageCombineMode.Replace); }

        /// <summary>
        /// Creates a test containing an assert representing a conditional AND that evaluates
        /// the right assert only if the assert of the initial instance is evaluated as true
        /// <para>By default ErrorMessage remains the same</para> 
        /// </summary>
        /// <param name="rightAssert"></param>
        /// <param name="ErrorMessage">Error message of generated Test object</param>
        /// <returns></returns>
        public Test And(Expression<Func<bool>> rightAssert, string ErrorMessage) { return And(new Test(rightAssert, ErrorMessage), MessageCombineMode.Replace); }
    };

    /// <summary>
    /// Class representing a Queue collection of Test. Tests can be passed through constructor or Add method and be executed with the Exec method
    /// </summary>
    public class TestSuite : IEnumerable<Test>
    {
        /// Initializes a new instance of TestSuite that is empty and has the default initial capacity
        /// </summary>
        public TestSuite() { _testQueue = new Queue<Test>(); }

        /// <summary>
        /// Initializes a new instance of the Queue<T> class
        /// that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="testEnum"></param>
        public TestSuite(IEnumerable<Test> testEnum)
        {
            _testQueue = new Queue<Test>(testEnum);
        }

        /// <summary>
        /// Execs all Tests contained in TestSuite in insertion order until one fails
        /// </summary>
        /// <param name="firstErrorMessage"></param>
        /// <returns></returns>
        public bool Exec(out string firstErrorMessage)
        {
            var firstFail = _testQueue.FirstOrDefault(x => x.Exec() == false);
            firstErrorMessage = firstFail?.ErrorMessage;
            return firstFail == default(Test);
        }

        /// <summary>
        /// Private Queue containing all Tests of TestSuite
        /// </summary>
        private Queue<Test> _testQueue;

        /// <summary>
        /// Return intern Test Queue enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Test> GetEnumerator() { return _testQueue.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Add a new Test in TestSuite
        /// </summary>
        /// <param name="test"></param>
        public void Add(Test test) { _testQueue.Enqueue(test); }
    }
}
