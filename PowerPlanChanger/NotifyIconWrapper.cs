using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace PowerPlanChanger
{
    public class NotifyIconWrapper
    {
        private readonly NotifyIcon _icon;
        private readonly FieldInfo _textField;
        private readonly FieldInfo _addedField;
        private readonly MethodInfo _updateIconMethod;

        public NotifyIconWrapper(NotifyIcon icon)
        {
            _icon = icon;
            const BindingFlags privateFieldFlag = BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = typeof(NotifyIcon);
            _textField = type.GetField("text", privateFieldFlag);
            _addedField = type.GetField("added", privateFieldFlag);
            _updateIconMethod = type.GetMethod("UpdateIcon", privateFieldFlag);
        }

        public string Text
        {
            get
            {
                return _icon.Text;
            }
            set
            {
                if (value == null) value = string.Empty;
                if (value.Length > 127) throw new ArgumentException("text");
                if (value.Length > 63)
                {
                    _textField.SetValue(_icon, value);
                    if ((bool)_addedField.GetValue(_icon))
                    {
                        _updateIconMethod.Invoke(_icon, new object[] {true});
                    }
                }
                else
                {
                    _icon.Text = value;
                }
            }
        }

        public Icon Icon
        {
            get
            {
                return _icon.Icon;
            }
            set
            {
                _icon.Icon = value;
            }
        }
    }
}
