using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace OrangeCat.Tests {
public sealed class TitleScreenTests {
    [UnityTest]
    public IEnumerator TitleDancesToMusicAndStartsCampaign() {
        Assert.That(SceneUtility.GetScenePathByBuildIndex(0), Does.EndWith("/TitleScreen.unity"));
        yield return SceneManager.LoadSceneAsync(0);
        var source = GameObject.Find("Title BGM").GetComponent<AudioSource>();
        var hao = GameObject.Find("HaoJu dance").GetComponent<SpriteRenderer>();
        var jiang = GameObject.Find("JiangZhua dance").GetComponent<SpriteRenderer>();
        var haoStart = hao.sprite;
        var jiangStart = jiang.sprite;
        bool haoMoved = false, jiangMoved = false;
        float timeout = Time.realtimeSinceStartup + 2.6f;
        while ((!haoMoved || !jiangMoved) && Time.realtimeSinceStartup < timeout) {
            haoMoved |= hao.sprite != haoStart;
            jiangMoved |= jiang.sprite != jiangStart;
            yield return null;
        }
        Assert.That(haoMoved && jiangMoved, Is.True);
        Assert.That(source.isPlaying && source.loop && source.timeSamples > 0, Is.True);

        var background = InputSystem.settings.backgroundBehavior;
#if UNITY_EDITOR
        var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.M));
            for (int i = 0; i < 30 && !source.mute; i++) yield return null;
            Assert.That(source.mute && source.isPlaying, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            timeout = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < timeout && SceneManager.GetActiveScene().name != "Warehouse01")
                yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Warehouse01"));
            Assert.That(source == null, Is.True);
            Assert.That(Object.FindFirstObjectByType<WarehouseRun>().Player, Is.Not.Null);
        } finally {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = routing;
#endif
        }
    }
}
}
