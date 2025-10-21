// 自动生成结构体 - 不要手动修改
// 文件夹源: Assets/Audio/BGM
// 生成日期: 2025-09-23 22:09:43

namespace AudioRegistry
{
    public readonly struct BGM : IAudio
    {
        public string Value { get; }
        
        private BGM(string value) => Value = value;
        
        public static BGM Zephyrs => new BGM("Zephyrs");
        
        public static implicit operator string(BGM id) => id.Value;
        
    }
}
