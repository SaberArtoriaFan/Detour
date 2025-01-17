#if UNITY_EDITOR
using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;
namespace Detour
{
    public static class DetourUtility
    {
        /// <summary> Returns the get accessor MethodInfo obtained from a method call expression. </summary>
        public static MethodInfo MethodInfoForMethodCall(Expression<Action> methodCallExpression)
        {
            var methodCall = methodCallExpression.Body as MethodCallExpression;
            if (methodCall != null)
            {
                return methodCall.Method;
            }
            else
            {
                throw new Exception($"Couldn't obtain MethodInfo for the method call expression: {methodCallExpression}");
            }
        }

        /// <summary> Returns the get accessor MethodInfo obtained from a property expression. </summary>
        public static MethodInfo MethodInfoForGetter<T>(Expression<Func<T>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression != null)
            {
                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo != null)
                {
                    return propertyInfo.GetMethod;
                }
            }
            throw new Exception($"Couldn't obtain MethodInfo for the property get accessor expression: {propertyExpression}");
        }

        /// <summary> Returns the set accessor MethodInfo obtained from a property expression. </summary>
        public static MethodInfo MethodInfoForSetter<T>(Expression<Func<T>> propertyExpression)
        {
            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression != null)
            {
                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (propertyInfo != null)
                {
                    return propertyInfo.SetMethod;
                }
            }
            throw new Exception($"Couldn't obtain MethodInfo for the property set accessor expression: {propertyExpression}");
        }

        public static unsafe void TryDetourFromTo(MethodInfo src, MethodInfo dst)
        {
            try
            {
                if (IntPtr.Size == sizeof(Int64))
                {
                    // 64-bit systems use 64-bit absolute address and jumps
                    // 12 byte destructive

                    // Get function pointers
                    long srcBase = src.MethodHandle.GetFunctionPointer().ToInt64();
                    long dstBase = dst.MethodHandle.GetFunctionPointer().ToInt64();

                    // Native source address
                    byte* pointerRawSource = (byte*)srcBase;

                    // Pointer to insert jump address into native code
                    long* pointerRawAddress = (long*)(pointerRawSource + 0x02);

                    // Insert 64-bit absolute jump into native code (address in rax)
                    // mov rax, immediate64
                    // jmp [rax]
                    *(pointerRawSource + 0x00) = 0x48;
                    *(pointerRawSource + 0x01) = 0xB8;
                    *pointerRawAddress = dstBase; // ( pointerRawSource + 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 )
                    *(pointerRawSource + 0x0A) = 0xFF;
                    *(pointerRawSource + 0x0B) = 0xE0;
                }
                else
                {
                    // 32-bit systems use 32-bit relative offset and jump
                    // 5 byte destructive

                    // Get function pointers
                    int srcBase = src.MethodHandle.GetFunctionPointer().ToInt32();
                    int dstBase = dst.MethodHandle.GetFunctionPointer().ToInt32();

                    // Native source address
                    byte* pointerRawSource = (byte*)srcBase;

                    // Pointer to insert jump address into native code
                    int* pointerRawAddress = (int*)(pointerRawSource + 1);

                    // Jump offset (less instruction size)
                    int offset = dstBase - srcBase - 5;

                    // Insert 32-bit relative jump into native code
                    *pointerRawSource = 0xE9;
                    *pointerRawAddress = offset;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unable to detour: {src?.Name ?? "null src"} -> {dst?.Name ?? "null dst"}\n{ex}");
                throw;
            }
        }
    }


    internal static class ProjectBrowser_CreateDropdown
    {
        //static GUIContent m_CreateDropdownContent;
        //static GUIStyle m_CreateDropdownStyle;
        static void Replacement(this UnityEditor.Editor editor)
        {
            //if(m_CreateDropdownStyle==null)
            //    m_CreateDropdownStyle = GetStyle("ToolbarCreateAddNewDropDown");

            //Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.IconContent("CreateAddNew"), m_CreateDropdownStyle);
            //Debug.Log("R1--》"+r);
            Rect r2 = GUILayoutUtility.GetRect(250, 20);
            //Debug.Log("R1--》" + r2);
            // 调用计算距离下午6点的剩余时间函数
            TimeSpan remainingTime = GetTimeUntilSixPM();
            DisplayTimeRemaining(r2, remainingTime);
        }
        static void DisplayTimeRemaining(Rect r, TimeSpan remainingTime)
        {
            // 格式化时间字符串
            string timeText;

            if (remainingTime.TotalHours >= 1)
            {
                timeText = $"{remainingTime.Hours}小时 {remainingTime.Minutes}分钟 {remainingTime.Seconds}秒";
            }
            else if (remainingTime.TotalMinutes >= 1)
            {
                timeText = $"{remainingTime.Minutes}分钟 {remainingTime.Seconds}秒";
            }
            else
            {
                timeText = $"{remainingTime.Seconds}秒";
            }

            // 设置文本颜色
            if (remainingTime.TotalHours < 0) // 超过18点，显示红色
            {
                GUI.contentColor = Color.red;
                timeText = "请下班"; // 显示"请下班"
            }
            else
            {
                GUI.contentColor = Color.green; // 恢复默认颜色
            }

            // 显示标签
            EditorGUI.LabelField(r, $"距离下班还有: {timeText}");

            // 恢复默认颜色
            GUI.contentColor = Color.white;
        }
        static TimeSpan GetTimeUntilSixPM()
        {
            // 获取当前时间
            DateTime currentTime = DateTime.Now;

            // 获取今天下午6点的时间
            DateTime sixPM = currentTime.Date.AddHours(18); // 当前日期的18:00

            // 计算与下午6点的时间差
            if (currentTime > sixPM)
            {
                // 如果当前时间已经超过下午6点，则返回负值或第二天的6点
                sixPM = sixPM.AddDays(1); // 计算明天的6点
            }

            // 返回剩余时间
            return sixPM - currentTime;
        }
        static GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
            }
            return s;
        }
        public static void PatchMethod()
        {
            var type = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            //m_CreateDropdownContent =type.GetNestedType("Styles")
            // get the MethodInfo for the method we're trying to patch
            // (it is private, so we need to dig around w/ reflection)
            var srcMethod
                =  // format: Namespace.Class+NestedClass,Assembly
                   type.GetMethod("CreateDropdown", BindingFlags.NonPublic | BindingFlags.Instance); // make sure you use correct binding flags!

            // get the MethodInfo for the replacement method (set accessor)
            var dstMethod
                = DetourUtility.MethodInfoForMethodCall(() => Replacement(null));

            // patch the method function pointer
            DetourUtility.TryDetourFromTo(
                src: srcMethod,
                dst: dstMethod
            );

            // now take a look at the Animator inspector...
        }
    }

    internal static class PlayModeButtons
    {

        public static void PatchMethod()
        {
            EditorApplication.update -= OnHook;

            EditorApplication.update += OnHook;

            // now take a look at the Animator inspector...
        }
        private static void OnHook()
        {
            Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            var type = Type.GetType("UnityEditor.Toolbars.PlayModeButtons,UnityEditor");
            var field = type.GetField("m_PlayButton", BindingFlags.NonPublic | BindingFlags.Instance);
            ScriptableObject m_currentToolbar=null;
            VisualElement rootElement = null;
            // 获取当前的UI文档根元素
            if (m_currentToolbar == null)
            {
                // Find toolbar
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (m_currentToolbar != null)
                {
                    var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
                    var rawRoot = root.GetValue(m_currentToolbar);
                    var mRoot = rawRoot as VisualElement;
                    rootElement = mRoot;

                }
            }
            if (rootElement == null)
            {
                Debug.Log("根节点不在");
                return;
            }
            
            // 在UI树中查找所有的PlayModeButtons元素
            var playModeButtons = PrintAllDescendants(rootElement, 1);

            if (playModeButtons != null)
            {
                var btn = (EditorToolbarToggle)field.GetValue(playModeButtons);
                //Debug.Log("找到了 PlayModeButtons 对象！"+btn);
                btn.onIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DefaultResources/Icon/d_Dota.png");
                btn.offIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/DefaultResources/Icon/d_LOL.png");

                // 在这里可以操作找到的PlayModeButtons对象
            }
            else
            {
                return;
            }
            EditorApplication.update -= OnHook;
        }
        // 递归遍历并打印所有子孙节点
        static VisualElement PrintAllDescendants(VisualElement element, int level)
        {
            // 打印当前节点信息
            //string indentation = new string(' ', level * 2); // 通过缩进显示层级
            if(element.name== "PlayMode")
            {
                //Debug.Log($"{indentation}{element.GetType().Name}");
                return element;
            }
            // 遍历子节点
            foreach (var child in element.Children())
            {
              var vm=  PrintAllDescendants(child, level + 1); // 递归调用以遍历孙子节点等
                if (vm != null)
                    return vm;
            }
            return null;
        }
    }

    [InitializeOnLoad]
    internal class Hook
    {
        static Hook()
        {
            ProjectBrowser_CreateDropdown.PatchMethod();
            PlayModeButtons.PatchMethod();
        }
    }
}

#endif
