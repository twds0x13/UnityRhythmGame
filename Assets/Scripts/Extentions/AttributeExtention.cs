using UnityEngine;

public class Ext
{
    public class ReadOnlyAttribute : PropertyAttribute // �ǵð�ע��д�ڹ��캯������
    {
        /// <summary>
        /// ������ <see cref="InspectorWindow"/> �ڲ��ܱ��༭��ע�ⲻҪ�� <see cref="Unity.Collections.ReadOnlyAttribute"/> ����
        /// </summary>
        public ReadOnlyAttribute() { }
    }

    public class ReadOnlyInGameAttribute : PropertyAttribute // �ǵð�ע��д�ڹ��캯������
    {
        /// <summary>
        /// ������ <see cref="InspectorWindow"/> �ڿ��Ա��༭��������ʱ���ܴ� <see cref="InspectorWindow"/> �б༭
        /// </summary>
        public ReadOnlyInGameAttribute() { }
    }
}
