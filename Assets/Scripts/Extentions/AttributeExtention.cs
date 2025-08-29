using UnityEngine;

public class Ext
{
    public class ReadOnlyAttribute : PropertyAttribute // 记得把注释写在构造函数上面
    {
        /// <summary>
        /// 代表在 <see cref="InspectorWindow"/> 内不能被编辑，注意不要和 <see cref="Unity.Collections.ReadOnlyAttribute"/> 混淆
        /// </summary>
        public ReadOnlyAttribute() { }
    }

    public class ReadOnlyInGameAttribute : PropertyAttribute // 记得把注释写在构造函数上面
    {
        /// <summary>
        /// 代表在 <see cref="InspectorWindow"/> 内可以被编辑，在运行时不能从 <see cref="InspectorWindow"/> 中编辑
        /// </summary>
        public ReadOnlyInGameAttribute() { }
    }
}
