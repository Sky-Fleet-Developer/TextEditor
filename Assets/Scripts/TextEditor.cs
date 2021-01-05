using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class TextEditor : MonoBehaviour
{
    public GUISkin textSkin;
    public Texture toggleTrue;
    public static Texture ToggleTrue;
    public Texture toggleFalse;
    public static Texture ToggleFalse;

    [System.Serializable]
    public class Text
    {
        public string correct_text;
        public string text_with_mistakes;
        [NonSerialized] public string testing_text;
        [NonSerialized] public State currentState = State.Write_Default;
        public string[] words;
        [NonSerialized] public bool[] mistakes;
        [NonSerialized] public int[] highlight;

        [NonSerialized] public bool editMode = true;

        private Vector2 scroll;

        public enum State
        {
            Write_Default = 0,
            Write_Mistakes = 1,
            Correction = 2
        }

        public enum Tab
        {
            None = 0,
            Space = 1,
            SingleSpace = 2
        }
        public string fileName;
        public void DrawEditor(Rect rect)
        {
            GUI.Box(rect, "");
            GUILayout.BeginArea(rect);
            string current;
            switch (currentState)
            {
                case State.Write_Default:

                    scroll = GUILayout.BeginScrollView(scroll, false, true);
                    correct_text = GUILayout.TextArea(correct_text);
                    GUILayout.EndScrollView();

                    if (GUILayout.Button("Сохранить исходный текст"))
                    {
                        text_with_mistakes = correct_text;
                        currentState = State.Write_Mistakes;
                        words = correct_text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    break;
                case State.Write_Mistakes:
                    current = text_with_mistakes;

                    scroll = GUILayout.BeginScrollView(scroll, false, true);
                    current = GUILayout.TextArea(current);
                    GUILayout.EndScrollView();

                    text_with_mistakes = current;

                    if (GUILayout.Button("Вернуться к исходному тексту"))
                    {
                        currentState = State.Write_Default;
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Запустить тестирование"))
                    {
                        currentState = State.Correction;
                        BakeTestingText();
                    }
                    if (GUILayout.Button("Сохранить в файл"))
                    {
                        SaveToFile(fileName);
                    }
                    fileName = GUILayout.TextField(fileName);
                    GUILayout.EndHorizontal();
                    break;
                case State.Correction:

                    current = testing_text;
                    scroll = GUILayout.BeginScrollView(scroll, false, true);
                    current = GUILayout.TextArea(current);
                    GUILayout.EndScrollView();

                    if (current != testing_text)
                    {
                        if(CheckMistakes(testing_text, current)) testing_text = current;
                    }

                    bool allTrue = true;
                    GUILayout.BeginHorizontal();
                    for(int i = 0; i < mistakes.Length; i++)
                    {
                        allTrue &= !mistakes[i];
                    }
                    if (allTrue)
                    {
                        GUILayout.Label("Готово!");
                    }
                    else
                    {
                        for (int i = 0; i < mistakes.Length; i++)
                        {
                            GUI.color = Color.Lerp(Color.white, highlight[i] > 0 ? Color.green : Color.red, Mathf.Abs(highlight[i] / 1000f));

                            if (highlight[i] != 0)
                            {
                                highlight[i] += highlight[i] > 0 ? -1 : 1;
                            }

                            GUILayout.Label(mistakes[i] ? ToggleFalse : ToggleTrue, GUILayout.Width(30));
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (editMode)
                    {
                        if (GUILayout.Button("Вернуться к редактированию"))
                        {
                            currentState = State.Write_Mistakes;
                        }
                    }

                    break;
            }
            GUILayout.EndArea();
        }

        private void BakeTestingText()
        {
            testing_text = string.Empty;
            Tab tab = Tab.None;
            for (int i = 0; i < text_with_mistakes.Length; i++)
            {
                if (text_with_mistakes[i] == '\\' || text_with_mistakes[i] == '/')
                {
                    if (tab == Tab.None)
                    {
                        switch (text_with_mistakes[i + 1])
                        {
                            case '-':
                                tab = Tab.Space;
                                break;
                            case '_':
                                tab = Tab.SingleSpace;
                                testing_text += '_';
                                break;
                        }
                        i++;
                    }
                    else
                    {
                        tab = Tab.None;
                        continue;
                    }
                }
                else
                {
                    switch (tab)
                    {
                        case Tab.None:
                            testing_text += text_with_mistakes[i];
                            break;
                        case Tab.Space:
                            testing_text += '_';
                            break;
                        case Tab.SingleSpace:
                            continue;
                    }
                }
            }
            CheckMistakes(testing_text, testing_text);
        }

        private bool CheckMistakes(string original, string text)
        {
            var o_words = original.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var t_words = text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (o_words.Length == t_words.Length)
            {

                if (highlight == null || highlight.Length == 0) highlight = new int[words.Length];

                mistakes = new bool[words.Length];
                bool[] chainges = new bool[t_words.Length];
                for (int i = 0; i < t_words.Length; i++)
                {
                    mistakes[i] = t_words[i] != words[i];
                    chainges[i] = o_words[i] != t_words[i];

                    if (chainges[i])
                    {
                        highlight[i] = mistakes[i] ? -1000 : 1000;
                    }
                }
                return true;
            }
            else return false;
        }

        public void ChaingesCheck(string original, string chainged, Action<int, int> onMestake)
        {
            int deltaLength = original.Length - chainged.Length;
            for(int i = 0; i < original.Length; i++)
            {
                if(original[i] != chainged[i])
                {
                    int r;
                    for(r = original.Length - 1; r > i; r--)
                    {
                        if(original[r] != chainged[r - deltaLength])
                        {
                            break;
                        }
                    }
                    onMestake?.Invoke(i, r);
                    return;
                }
            }
        }

        public void SaveToFile(string name)
        {
            string json = JsonUtility.ToJson(this);

            string path = Application.dataPath + "/" + name + ".txt";

            if (File.Exists(path))
            {
                DoPopup("Файл с таким именем уже существует. Перезаписать?", v =>
                {
                    if (v)
                    {
                        File.Delete(path);
                        WriteFile(json, path);
                    }
                });
            }
            else
            {
                WriteFile(json, path);
            }
        }

        private static void WriteFile(string json, string path)
        {
            var stream = File.Create(path);
            var bytes = Encoding.Default.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
        }
    }

    public class Popup
    {
        public string content;
        public Action<bool> answer;

        public void Draw()
        {
            GUI.Box(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 70, 400, 140), content);
            if (GUI.Button(new Rect(Screen.width / 2, Screen.height / 2 + 40, 90, 25), "Да"))
            {
                answer?.Invoke(true);
                defaultPopup = null;
            }
            if (GUI.Button(new Rect(Screen.width / 2 + 100, Screen.height / 2 + 40, 90, 25), "Нет"))
            {
                answer?.Invoke(false);
                defaultPopup = null;
            }
        }
    }

    public static Popup defaultPopup;

    public static void DoPopup(string content, Action<bool> answer)
    {
        defaultPopup = new Popup { content = content, answer = answer };
    }

    public Text currentText;

    private void Start()
    {
        ToggleTrue = toggleTrue;
        ToggleFalse = toggleFalse;
    }

    public void OnGUI()
    {
        if (defaultPopup != null)
        {
            defaultPopup.Draw();
            return;
        }
        GUI.skin = textSkin;
        currentText.DrawEditor(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
    }
}
