using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using static PhenoWareCommon.CommonFunctions;


namespace PhenoWareCommon
{
    public static class CommonMethodsExtensions
    {
        /// <summary>
        /// return the DescriptionAttribute value of e if is an enum. If e is not an enum or DescriptionAttribute is null return an empty string
        /// </summary>
        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// Determines whether the collection is null or contains no elements.
        /// </summary>
        /// <typeparam name="T">The IEnumerable type.</typeparam>
        /// <param name="enumerable">The enumerable, which may be null or empty.</param>
        /// <returns>
        ///     <c>true</c> if the IEnumerable is null or empty; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }
            /* If this is a list, use the Count property for efficiency. 
             * The Count property is O(1) while IEnumerable.Count() is O(N). */
            var collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                return collection.Count < 1;
            }
            return !enumerable.Any();
        }

        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            // need to detect whether they use the same
            // parameter instance; if not, they need fixing
            ParameterExpression param = expr1.Parameters[0];
            if (ReferenceEquals(param, expr2.Parameters[0]))
            {
                // simple version
                return Expression.Lambda<Func<T, bool>>(
                    Expression.AndAlso(expr1.Body, expr2.Body), param);
            }
            // otherwise, keep expr1 "as is" and invoke expr2
            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(
                    expr1.Body,
                    Expression.Invoke(expr2, param)), param);
        }

        /// <summary>
        /// store a clear method associated with each type of control handled by ClearControl extension method
        /// </summary>
        private static Dictionary<Type, Action<Control>> controldefaults = new Dictionary<Type, Action<Control>>()
        {
            {typeof(TextBox), c =>
                {
                    TextBox textBox = ((TextBox)c);
                    textBox.Clear();
                    textBox.BackColor = default(Color);
                }
            },
            {typeof(CheckedListBox), c =>  ((CheckedListBox)c).SetAll(false)},
            {typeof(NumericUpDown), c =>  ((NumericUpDown)c).Value = ((NumericUpDown)c).Minimum},
            {typeof(ComboBox), c => ((ComboBox)c).Text = String.Empty},
            {typeof(CheckBox), c => ((CheckBox)c).Checked = false},
            {typeof(ListBox), c => ((ListBox)c).Items.Clear()},
            {typeof(ListView), c => ((ListView)c).Items.Clear()},
            {typeof(RadioButton), c => ((RadioButton)c).Checked = false},
            {typeof(GroupBox), c => ((GroupBox)c).Controls.ClearControlCollection()},
            {typeof(Panel), c => ((Panel)c).Controls.ClearControlCollection()},
        };

        /// <summary>
        /// Check if control type has an associated clear method and invoke it it's the case
        /// </summary>
        /// <param name="type"></param>
        /// <param name="control"></param>
        private static void FindAndInvokeControlDefaultClear(Type type, Control control)
        {
            if (controldefaults.ContainsKey(type))
            {
                setControlThreadGuiSafe(control, controldefaults[type]);
            }
        }

        /// <summary>
        /// set all Items of CheckedListBox to value
        /// </summary>
        /// <param name="checkListBox"></param>
        /// <param name="value"></param>
        public static void SetAll(this CheckedListBox checkListBox, bool value)
        {
            foreach (var i in Enumerable.Range(0, checkListBox.Items.Count))
                checkListBox.SetItemChecked(i, value);
        }

        /// <summary>
        /// generic control clear method applied for each element of ControlCollection
        /// </summary>
        /// <param name="controls"></param>
        public static void ClearControlCollection(this Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                FindAndInvokeControlDefaultClear(control.GetType(), control);
            }
        }

        /// <summary>
        /// generic control clear method applied for each element of IEnumerable<Control>
        /// </summary>
        /// <param name="controls"></param>
        public static void ClearControls(this IEnumerable<Control> controls)
        {
            foreach (Control control in controls)
            {
                FindAndInvokeControlDefaultClear(control.GetType(), control);
            }
        }

        public static void ClearControl(this Control control)
        {
            FindAndInvokeControlDefaultClear(control.GetType(), control);
        }

    }
}
