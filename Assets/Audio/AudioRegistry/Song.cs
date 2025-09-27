// 自动生成结构体 - 不要手动修改
// 文件夹源: Assets/Audio/Song
// 生成日期: 2025-09-23 22:09:43

namespace AudioRegistry
{
    public readonly struct Song : IAudio
    {
        public string Value { get; }
        
        private Song(string value) => Value = value;
        
        public static Song 天使の帰郷 => new Song("天使の帰郷");
        
        public static implicit operator string(Song id) => id.Value;
        
    }
}
