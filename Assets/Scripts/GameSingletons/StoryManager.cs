using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Singleton;
using UnityEngine;
using Audio = AudioNS.AudioManager;
using Json = JsonLoader.JsonManager;

namespace StoryNS
{
    [SerializeField]
    public class StoryContainer
    {
        public int Id = 0;

        public int Depth = 0;

        public int ChapterNumber = 0;

        public int NodeNumber = 0;

        public int LineNumber = 0;

        [JsonIgnore]
        public StoryNode CurChapter;

        [JsonIgnore]
        public StoryNode CurNode;

        [JsonIgnore]
        public StoryNode CurLine;

        [JsonIgnore]
        public List<StoryNode> Parallel;

        public List<StoryNode> StoredNodes;

        public StoryContainer()
        {
            StoredNodes = new();
            Parallel = new();
        }

        public void JumpTest((int Chapter, int Node, int Line) Destination)
        {
            try
            {
                CurChapter = StoredNodes[Destination.Chapter];
                CurNode = CurChapter.Nodes[Destination.Node];
                CurLine = CurNode.Nodes[Destination.Line];
            }
            catch (Exception Ex)
            {
                throw new ArgumentNullException(
                    $"Invalid Jump Destination ({Destination.Chapter},{Destination.Node},{Destination.Line}) ",
                    Ex
                );
            }
        }

        public void StoryTest()
        {
            for (int i = 1; i < 5; i++)
            {
                var Chapter = new StoryNode();

                Chapter.Name = "Chapter " + i;

                Chapter.Depth = 1;

                Chapter.Id = i;

                Parallel.Add(Chapter.ClearRef());

                for (int j = 1; j < 5; j++)
                {
                    var Node = new StoryNode();

                    Node.Name = "Node " + j;

                    Node.Depth = 2;

                    Node.Id = j;

                    Parallel.Add(Node.ClearRef());

                    for (int k = 1; k < 5; k++)
                    {
                        var Line = new StoryNode();

                        Line.Name = "Line " + k;

                        Line.Depth = 3;

                        Line.Id = k;

                        Line.Actions.Add(new StoryJumpAction((1, 1, 4)));

                        Parallel.Add(Line);

                        Node.Nodes.Add(Line);
                    }

                    Chapter.Nodes.Add(Node);
                }

                StoredNodes.Add(Chapter);
            }
        }

        public void ReadTest()
        {
            foreach (StoryNode Chapter in StoredNodes)
            {
                foreach (StoryNode Node in Chapter.Nodes)
                {
                    foreach (StoryNode Line in Node.Nodes)
                    {
                        Debug.Log(Line.Name);
                    }
                }
            }
        }

        public void ActionTest()
        {
            foreach (StoryNode Chapter in StoredNodes)
            {
                foreach (StoryNode Node in Chapter.Nodes)
                {
                    foreach (StoryNode Line in Node.Nodes)
                    {
                        Line.InvokeActions();
                    }
                }
            }
        }

        public void ParallelReadTest()
        {
            for (int i = 0; i < Parallel.Count; i++)
            {
                Debug.Log(Parallel[i].Name);
            }
        }

        public string GetLocalizationKey(int Chapter, int Node, int Line)
        {
            return "C" + Chapter + "_N" + Node + "_L" + Line;
        }
    }

    public enum NodeType : byte // 不要动这里的顺序，强制转换要用
    {
        Null,
        Chapter,
        Node,
        Line,
    }

    /// <summary>
    /// Chapter - Node - Line 和单级 Node 结构实现完全等价
    /// </summary>
    [Serializable]
    public class StoryNode
    {
        public int Id { get; set; }

        public int Depth { get; set; }

        public NodeType Type
        {
            get { return (NodeType)Depth; }
        }

        public string Name { get; set; }

        [JsonIgnore]
        public StoryNode CurNode { get; set; }

        public List<StoryAction> Actions { get; protected set; }

        public List<StoryNode> Nodes { get; set; }

        public StoryNode()
        {
            Nodes = new();
            Actions = new();
        }

        public void FirstNode()
        {
            if (Nodes[0] is not null)
            {
                CurNode = Nodes[0];
            }
            else
            {
                throw new ArgumentNullException("Nodes Empty");
            }
        }

        public void NextNode()
        {
            if (Nodes[CurNode.Id + 1] is not null)
            {
                CurNode = Nodes[CurNode.Id + 1];
            }
            else
            {
                this?.InvokeActions(); // 这里假定我们用 StoryJumpAction 跳转到下一个节点
            }
        }

        public StoryNode ClearRef()
        {
            StoryNode Node = new StoryNode();

            Node.Id = Id;

            Node.Depth = Depth;

            Node.Name = Name;

            return Node;
        }

        public void InvokeActions()
        {
            if (Actions.Count > 0)
            {
                foreach (var Action in Actions)
                {
                    Action?.Invoke();
                }
            }
        }
    }

    public enum ActionType : byte
    {
        Null,
        Log,
        LogMerged,
        StoryJump,
        PlayAudio,
    }

    public interface IStoryAction // 这个接口看似没用 但是神秘的解决了空引用 Bug 令人忍俊不禁
    {
        public ActionType Type { get; set; }

        public Dictionary<string, object> Params { get; set; }

        public void SafeAdd<T>(string Key, T Value);

        public T SafeGet<T>(T Default = default);

        public T SafeGet<T>(string Key, T Default = default);
    }

    [Serializable]
    public class StoryAction : IStoryAction
    {
        public ActionType Type { get; set; }

        /// <summary>
        /// 在默认情况下，你应该把结构化的数据，如 <see cref="List{T}"/> 或 <see cref="Tuple{T1, T2, T3}"/> 等作为一个 <see cref="object"/> 存入字典，默认键值为 <see cref="Type"/>。
        /// 你也可以用多个键存入多个结构化数据。
        /// </summary>
        public Dictionary<string, object> Params { get; set; } // 大位袋这一块/.

        /// <summary>
        /// 往 <see cref="Params"/> 字典中添加一个 <typeparamref name="T"/> 类型对象并自动转为 <see cref="object"/> 对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <exception cref="ArgumentException"></exception>
        public void SafeAdd<T>(string Key, T Value)
        {
            if (string.IsNullOrEmpty(Key))
                throw new ArgumentException("Key cannot be null or empty");

            Params[Key] = Value;
        }

        /// <summary>
        /// <see cref="SafeGet{T}(string, T)"/> 的精简版本，默认键值为 <see cref="Type"/> 值的 <see cref="Enum.ToString()"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Default"></param>
        /// <returns></returns>
        public T SafeGet<T>(T Default = default) // 卧槽 这个函数太有智慧了
        {
            return SafeGet(Type.ToString(), Default);
        }

        /// <summary>
        /// 安全的将 <see cref="Params"/> 中的 <see cref="object"/> 对象通过 <see cref="Newtonsoft.Json"/> 库转换回所需的 <typeparamref name="T"/> 类型对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="Default"></param>
        /// <returns></returns>
        public T SafeGet<T>(string Key, T Default = default)
        {
            if (Params.TryGetValue(Key, out object Value))
            {
                if (Value is T TypeValue) // 无需转换
                {
                    return TypeValue;
                }

                if (Value is JToken JToken) // Newtonsoft.Json.Linq.JToken 安全转换
                {
                    try
                    {
                        return JToken.ToObject<T>();
                    }
                    catch (Exception Ex)
                    {
                        throw new JsonSerializationException(
                            $"Jtoken Type Casting Fail occured in {typeof(object)} to {typeof(T)} Type.",
                            Ex
                        );
                    }
                }

                try // 尝试强制转换
                {
                    return (T)Convert.ChangeType(Value, typeof(T));
                }
                catch (Exception Ex)
                {
                    throw new JsonSerializationException(
                        $"Type Casting Fail occured in {typeof(object)} to {typeof(T)} Type.",
                        Ex
                    );
                }
            }
            return Default;
        }

        protected StoryAction()
        {
            Params = new();
        }
    }

    [Serializable]
    public class LogAction : StoryAction
    {
        /// <summary>
        /// 在控制台内输出 <paramref name="StringArg"/> 字符串
        /// </summary>
        /// <param name="StringArgs"></param>
        public LogAction(string StringArg)
        {
            Type = ActionType.Log;
            SafeAdd(ActionType.Log.ToString(), StringArg);
        }
    }

    [Serializable]
    public class LogMergedAction : StoryAction
    {
        /// <summary>
        /// 在控制台内输出 <paramref name="StringArgs"/> 内所有字符串的 <see cref="StringBuilder.Append(string)"/> 后版本
        /// </summary>
        /// <param name="StringArgs"></param>
        public LogMergedAction(List<string> StringArgs)
        {
            Type = ActionType.LogMerged;
            SafeAdd(ActionType.LogMerged.ToString(), StringArgs);
        }
    }

    [Serializable]
    public class StoryJumpAction : StoryAction
    {
        /// <summary>
        /// 跳转到 <paramref name="Chapter"/> 章节 <paramref name="Node"/> 节点的 <paramref name="Line"/> 行
        /// </summary>
        /// <param name="Chapter"></param>
        /// <param name="Node"></param>
        /// <param name="Line"></param>
        public StoryJumpAction((int Chapter, int Node, int Line) Destination) // Destination 是元组类型！
        {
            Type = ActionType.LogMerged;
            SafeAdd(ActionType.LogMerged.ToString(), Destination);
        }
    }

    [Serializable]
    public class PlayAudioAction : StoryAction
    {
        /// <summary>
        /// 读取 <paramref name="StringArgs"/> 内的第一个字符串，设为游戏全局背景音乐
        /// </summary>
        /// <param name="StringArgs"></param>
        public PlayAudioAction(string[] StringArgs)
        {
            Type = ActionType.PlayAudio;
            SafeAdd(ActionType.PlayAudio.ToString(), StringArgs);
        }
    }

    public static class StoryActionHandler // 处理逻辑全部塞到这里
    {
        public static void Invoke(this IStoryAction Action)
        {
            // Logs
            switch (Action.Type)
            {
                case ActionType.Null:
                    Debug.LogWarning("Action Object Null !");
                    break;

                case ActionType.Log:
                    Debug.Log(Action.SafeGet<string>());
                    break;

                case ActionType.LogMerged:
                    StringBuilder Builder = new StringBuilder();
                    foreach (string item in Action.SafeGet<List<string>>())
                    {
                        Builder.Append(item);
                    }
                    Debug.Log(Builder.ToString());
                    break;
            }

            // Story
            switch (Action.Type)
            {
                case ActionType.StoryJump:
                    Debug.Log(Action.SafeGet<(int, int, int)>());
                    break;
            }

            // Sounds
            switch (Action.Type)
            {
                case ActionType.PlayAudio:
                    Audio.Inst.LoadAudioClip(Action.SafeGet<string>(), nameof(BackGroundMusic));
                    break;
            }
        }
    }

    /*
    public class StoryManager : Singleton<StoryManager>
    {
        public readonly string FileName = "GameStory.story";

        private StoryContainer _storyContainer;

        public StoryContainer StoryContainer
        {
            get { return _storyContainer; }
            private set { _storyContainer = value; }
        }

        protected override void SingletonAwake()
        {
            StoryContainer = new();

            StoryContainer.StoryTest();

            WaitOneFrame(SaveStory).Forget();
        }

        public async UniTaskVoid WaitOneFrame(Action Function)
        {
            await UniTask.Yield();
            Function?.Invoke();
        }

        public void GetNextLine() { }

        public void GetTwoLine() { }

        public void SaveStory()
        {
            Json.TrySaveJsonToZip(FileName, StoryContainer);
        }

        private void LoadStory()
        {
            Json.TryLoadJsonFromZip(FileName, out _storyContainer);
        }
    }
    */
}
