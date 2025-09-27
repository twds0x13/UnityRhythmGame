// 自动生成结构体 - 不要手动修改
// 文件夹源: Assets/Audio/SFX
// 生成日期: 2025-09-23 22:09:43

namespace AudioRegistry
{
    public readonly struct SFX : IAudio
    {
        public string Value { get; }
        
        private SFX(string value) => Value = value;
        
        public static SFX key => new SFX("key");
        public static SFX Key1 => new SFX("Key1");
        public static SFX Key2 => new SFX("Key2");
        public static SFX Key3 => new SFX("Key3");
        
        public static implicit operator string(SFX id) => id.Value;
        
    }
}
