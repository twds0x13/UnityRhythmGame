using IUtils;
using NBehaviour;
using PooledObject;
using UnityEditor.UI;
using UnityEngine;
using Game = GameBehaviourManager.GameBehaviour;

namespace TBehaviour
{
    /// <summary>
    /// �� <see cref="NoteBehaviour"/> ����ճ�����޸ĵõ����������
    /// </summary>
    public class TrackBehaviour : PooledObjectBehaviour, IPooling, IDev
    {
        public bool isFake;

        public int TrackId;

        /// <summary>
        /// �����ʲôҲ����ӣ���Ҫ�� <see cref="null"/> �ؼ���, �� <see cref="PooledObjectBehaviour.Null"/>
        /// </summary>
        private void Awake() // ��Awakeʱ��ξʹ����ί�У�����OnEnable����״̬��ResetSelfHandler��
        {
            LogicUpdator += Null;
            AnimeDisappearUpdator += Disappear;
            AnimeQueueUpdator += Position;
            OnEnableHandler += RegisterReset; // �мǣ�OnEnale��ִ��˳���� ObjectPool �������в���֮ǰ
            AwakeHandler += Null;
        }

        public void RegisterReset() // �кܶණ����Ҫ����ʱ��ʼ���������GetComponent�õ���Ψһ TrackId��
        {
            LogicUpdator -= InvokeDestroy; // ��������ȳ�ʼ��������Ѿ��ù���ɾ���¼�����Ȼ������˳��NotePool�͸�ɾ��......

            // Debug.LogFormat("Pre:{0}", TrackId); // ������ TrackId ��δ������ʱ�޸ģ��̳���һ�α�Resetʱ��ֵ

            isDestroying = false;
            hasDisappearingAnime = false;

            PostResetLock = false; // ǿ�Ƶȴ�ȫ�� GetComponent<> �޸Ľ������ٽ��к��ʼ��

            DisappearingTimeSpan = 0.1f;
            DisappearingTimeCache = 0f;
            DeathTimeSpan = 0.5f;

            SpriteRenderer.sprite = SpriteList[0];
            SpriteRenderer.color = new Color(1f, 1f, 1f);
            transform.position = new Vector3(10f, 0f, 0f);

            LogicUpdator += ResetTrack; // ���ʼ���ӵ� Update ����ѭ������
        }

        public void ResetTrack()
        {
            if (PostResetLock)
            {
                // Debug.LogFormat("Post:{0}", TrackId); // ������ TrackId ��֤�Ѿ�����ʼ������ÿ�����ø����֮��϶������һ��

                if (TrackId < 4)
                    isDestroyAble = false;

                LogicUpdator -= ResetTrack; // ֻ����һ��
            }
        }

        public void Position()
        {
            CurT = Mathf.Pow(
                (Game.Inst.GetGameTime() - CurAnime.StartT) / CurAnime.TotalTimeElapse(),
                1f
            );

            transform.position = (1 - CurT) * CurAnime.StartV + CurT * CurAnime.EndV;
        }

        public bool Disappear()
        {
            CurT = DisappearingTimeCache + DisappearingTimeSpan - Game.Inst.GetGameTime(); // 0.2f -> 0f

            transform.position =
                DisappearingPosCache - new Vector3(0f, 3 * (DisappearingTimeSpan - CurT), 0f);

            SpriteRenderer.color = new Color(1f, 1f, 1f, CurT);

            return CurT != 0f;
        }

        public new void DevLog()
        {
            base.DevLog();

            Debug.Log("Note Object: ");

            AnimeQueue.TryPeek(out CurAnime);

            CurAnime.DevLog();
        }
    }
}
