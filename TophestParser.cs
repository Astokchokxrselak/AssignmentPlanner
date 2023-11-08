using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using Common;
using Common.UI;
using Common.Extensions;
using Common.Helpers;

public class TophestParser : MonoBehaviour
{
    public static TophestParser parser;
    OnButton onButton;
    public GameObject output;
    Dictionary<string, Sprite> fontset;
    private void Start()
    {
        if (parser == null)
        {
            FontOrder = Letters.Keys.ToArray();
            parser = this;
        }
        onButton = this.GetOrAddComponent<OnButton>();
        onButton.OnButtonClicked += () =>
        {
            Reparse();
        };
        SetFontset();

        var input = GetComponentInChildren<TMPro.TMP_InputField>();
        input.onSubmit.AddListener(
            text => {
                engWord = text; 
            }
        );
    }
    public class Syllable
    {
        public const int LetterCount = 2;
        public int ActiveCount => symbols.Count(c => c != null);
        public int Length => symbols.Count;
        public List<string> symbols;
        public Syllable()
        {
            this.symbols = new List<string>();
        }
        public override string ToString()
        {
            return symbols.ArrToString();
        }
        public string this[int i]
        {
            get
            {
                if (MathHelper.Between(i, 0, Length - 1))
                {
                    return symbols[i];
                }
                return null;
            }
            set
            {
                symbols[i] = value;
            }
        }
        // Returns false if a letter cannot be added or if the syllable has reached maximum capacity.
        private bool mergeNext = false;
        public bool TryAddLetter(string symbol, bool merge)
        {
            if (mergeNext) // If this letter is to be merged to the one previous
            {
                if (symbols.Count > 0) // If we have at least one existing symbol in the syllable
                {
                    symbols[^1] += symbol; // Add this symbol to the last symbol in this syllable.
                    mergeNext = false; // Disable merging with the preceding letter
                    return true; // We successfully added another letter to the syllable through appending to the previous symbol
                }
                else throw new System.Exception("MergeNext is set to true before symbols were ever added");
            }
            else if (Length < LetterCount) // Otherwise, if we have less than 2 symbols in this syllable...
            {
                symbols.Add(symbol); // Add the next symbol to symbols.
                if (merge) // If this is a merging character
                {
                    mergeNext = true; // Set the next symbol to merge with this one
                }
                return true;
            }
            return false; // Return false, as we cannot add anymore letters to this syllable.
        }
        public void ShiftEmptiesRight()
        {
            List<string> empties = new();
            for (int i = 0; i < symbols.Count; i++)
            {
                var symbol = symbols[i];
                if (symbol == null)
                {
                    empties.Add(symbol);
                    symbols.RemoveAt(i);
                }
            }
            for (int i = 0; i < empties.Count; i++)
            {
                symbols.Add(empties[i]);
            }
           
        }
    }
    public class Word
    {
        public int letterLength, capacity;
        public List<Syllable> syllables;
        public Word(int initialLength)
        {
            capacity = initialLength;
            syllables = new List<Syllable>(initialLength / Syllable.LetterCount);
        }
        public void AddLetter(string symbol)
        {
            if (syllables.Count == 0) // If we have no syllables in our word,
            {
                syllables.Add(new Syllable()); // add our first syllable.
            }
            if (syllables[^1].TryAddLetter(symbol, Letters[symbol].MergeSyllable)) // Try to add the next letter to the most recent syllable. If we are able to,
            {
                if (!Letters[symbol].MergeSyllable)
                {
                    letterLength++; // Add one to the amount of letters in this word.
                }
            }
            else // If we are unable to add the next letter to the most recent syllable,
            {
                syllables.Add(new Syllable()); // Add a new syllable.
                AddLetter(symbol); // Attempt again to add the same letter to our word.
            }
        }
        public bool TryAddLetter(string symbol)
        {
            if (syllables[^1].TryAddLetter(symbol, Letters[symbol].MergeSyllable)) // Try to add the next letter to the most recent syllable. If we are able to,
            {
                letterLength++; // Add one to the amount of letters in this word.
                return true;
            }
            return false;
        }
        public void Clean()
        {
            ShiftEmptySymbolsRight();
            ClearEmptySyllables();
            GroupSyllables();
            ClearEmptySymbols();
        }
        public void ShiftEmptySymbolsRight()
        {
            for (int i = 0; i < syllables.Count; i++)
            {
                syllables[i].ShiftEmptiesRight();
            }
        }
        public void ClearEmptySyllables()
        {
            for (int i = 0; i < syllables.Count; i++)
            {
                if (syllables[i].Length == 0 || syllables[i].ActiveCount == 0)
                {
                    syllables.RemoveAt(i--);
                }
            }
        }
        public void ClearEmptySymbols()
        {
            for (int i = 0; i < syllables.Count; i++)
            {
                if (syllables[i].ActiveCount != Syllable.LetterCount) // All syllables should have at least one active symbol since empties were removed
                {
                    for (int j = syllables[i].Length - 1; j > syllables[i].ActiveCount - 1; j--)
                    {
                        syllables[i].symbols.RemoveAt(j);
                    }
                }
            }
        }
        public void GroupSyllables() 
        {
            for (int i = 0; i < syllables.Count; i++) // Iterate over syllables.
            {
                if (syllables[i].symbols.Any(c => c == null) || syllables[i].Length != Syllable.LetterCount) // If this current syllable has not reached maximum capacity
                {
                    if (syllables[i].Length != Syllable.LetterCount)
                    {
                        var length = syllables[i].Length;
                        for (int j = 0; j < Syllable.LetterCount - length; j++) syllables[i].symbols.Add(null); // Ensure this syllable has an appropriate amount of symbols
                    }
                    if (i != syllables.Count - 1) // If this is not the last syllable
                    {
                        print("Next: " + syllables[i]);
                        if (syllables[i + 1].symbols.Any(c => c != null) || syllables[i + 1].Length != Syllable.LetterCount)
                        {
                            // let current = {a, d, ,}
                            // let other = {b, e, c, e}
                            // we want to traverse through current AND other
                            // pop first element of other and bring it to an empty element of current
                            // shift other elements
                            Syllable current = syllables[i], other = syllables[i + 1];
                            print(other.symbols.Any(c => c != null));
                            for (int i1 = 0; i1 < syllables[i].Length && other.symbols.Any(c => c != null); i1++)
                            {
                                print("i1: " + i1);
                                if (current[i1] == null) // If the symbol at i1 is empty
                                {
                                    // i1 = 2
                                    current[i1] = other[0]; // Assign the leftmost symbol of the proceeding syllable to its index
                                    // {a, d, b, }
                                    
                                    other[0] = null; // {, e, c, e}
                                    var old = other[0];
                                    for (int n = 0; n < other.Length - 1; n++) // Shift every symbol in the other syllable to the left
                                    {
                                        other[n] = other[n + 1];
                                        // {e, e, c, e}
                                        // {e, c, c, e}
                                        // {e, c, e, e}
                                    }
                                    other.symbols.RemoveAt(other.symbols.Count - 1); // Remove the symbol at the end of other
                                    if (other.Length == 0) // If the syllable is empty
                                    {
                                        syllables.RemoveAt(i-- + 1); // Remove it from our list of syllables
                                        break;
                                    }
                                    // {e, c, e,}
                                }
                            }
                        }
                    }
                }
            }
        }
        public void Replace()
        {

        }
        public string this[int i]
        {
            get
            {
                var index = i / 2;
                if (!MathHelper.Between(index, 0, syllables.Count - 1))
                {
                    throw new System.IndexOutOfRangeException(index + " is not in syllable range (0, " + syllables.Count + ").");
                }
                var syllable = syllables[i / 2];
                var letter = syllable[i - i / 2 * Syllable.LetterCount];
                return letter;
            }
            set
            {
                var index = i / 2;
                if (!MathHelper.Between(index, 0, syllables.Count - 1))
                {
                    throw new System.IndexOutOfRangeException(index + " is not in syllable range (0, " + syllables.Count + ").");
                }
                var syllable = syllables[i / 2];
                syllable[i - i / 2 * Syllable.LetterCount] = value;
            }
        }
        public string this[int i, int j]
        {
            get
            {
                var word = "";
                for (int n = i; n < j; n++)
                {
                    if (this[n] != null)
                    {
                        word += this[n];
                    }
                }
                return word;
            }
        }
        public Syllable LastSyllable => syllables[^1];
        public bool Match(int index, string line)
        {
            for (int i = letterLength; i > index + (line.Length - 1); i--)
            {
                // print("Attempted Line: " + this[index, i]);
                var substr = line.Substring(0, System.Math.Min(line.Length, i - index));
                print("Sub: " + substr + " compared to " + this[index, i]);
                // print(i - index);
                // print("Line Substring: " + substr);
                if (this[index, i] == substr)
                {
                    return true;
                }
            } return false;
        }
    }
    public abstract class Letter
    {
        public string symbol;
        public abstract void OnCreated(int index, RectTransform letter, ref Vector2 cursor);
        public virtual bool MergeSyllable => false;
    }
    public class TophestLetter : Letter
    {
        public TophestLetter(string symbol)
        {
            this.symbol = symbol;
        }
        public override void OnCreated(int index, RectTransform letter, ref Vector2 cursor)
        {
            if (index % 2 == 0)
            {
                cursor += Vector2.down * LetterSize;
            }
            else
            {
                cursor += Vector2.one * LetterSize;
            }
        }
    }
    public class TophestAccessory : Letter
    {
        public override bool MergeSyllable => true;
        public TophestAccessory(string symbol)
        {
            this.symbol = symbol;
        }
        public override void OnCreated(int index, RectTransform letter, ref Vector2 cursor)
        {

        }
    }
    private static string[] FontOrder;
    private static readonly Dictionary<string, Letter> Letters = new()
    {
        { "a", new TophestLetter("a") },
        { "b", new TophestLetter("b") },
        { "c",  new TophestLetter("c") },
        { "d", new TophestLetter("d") },
        { "e", new TophestLetter("e") },
        { "v", new TophestLetter("v") },
        { "i", new TophestLetter("i") },
        { "h", new TophestLetter("h") },
        { "o", new TophestLetter("o") },
        { "k", new TophestAccessory("k") },
        { "n", new TophestLetter("n") },
        { "t", new TophestLetter("t") },
        { "z", new TophestLetter("z") },
        { "uu", new TophestLetter("ü") },
        { "u", new TophestLetter("?") },
        { "m", new TophestLetter("m") },
        { "l", new TophestLetter("l") },
        { "q", new TophestLetter("q") },
        { "r", new TophestLetter("r") }
    };
    void SetFontset()
    {
        fontset = new();
        var font = SpriteHelper.GetTextureSprites("TophestCharacter");
        for (int i = 0; i < FontOrder.Length; i++)
        {
            var letter = FontOrder[i];
            fontset[letter] = font[i];
        }
    }
    public string engWord;
    // What do we wan
    void BreakIntoSyllables()
    {
        // Objective 1: Split up a word assuming all letters are monographic.
        // SubObjective: Add arrays
    }

    // Objective 1: Get size.
    // Take (word.Length + word.Length % Syllable.LetterCount) / Syllable.LetterCount and assign to length
    public const float LetterSize = 64f, LetterHeightOffset = 32f;


    // Objective 2: Output rectangles, centered around the center of the screen.
    // 1. Iterate over word
    // 2. For each character, instantiate a new image.
    // 3. Place the image ontop of the previous one

    // Objective 3: Reeplace rectangles with font according to letter.

    // Objective 4: Write the parser.
    // 1. Translate Python to C#
    // 2. Read over each generated list, adding each unique array to a new syllable
    Letter GetCharacter(int index) => Letters[FontOrder[index]];
    void SetLetterSprite(Image letter, string @char)
    {
        letter.sprite = fontset[@char.ToString()];
    } 
    void OutputRectangles()
    {
        Vector2 cursor = new(-LetterSize / 2 * (tWord.syllables.Count - 1), LetterHeightOffset);
        print("Letter Length: " + tWord.letterLength);
        for (int i = 0; i < tWord.letterLength; i++)
        {
            print("Index = " + i);
            var symbol = tWord[i];
            if (Letters.ContainsKey(symbol))
            {
                var letter = GetLetter(i, Letters[symbol], ref cursor);
                SetLetterSprite(letter, symbol);
            }
            else if (symbol != null)
            {
                for (int j = 0; j < symbol.Length; j++)
                {
                    var subsymbol = symbol[j].ToString();
                    var letter = GetLetter(i, Letters[subsymbol], ref cursor);
                    SetLetterSprite(letter, subsymbol);
                }
            }
        }
    }
    void DestroyOldRectangles()
    {
        foreach (Transform transform in output.transform)
        {
            Destroy(transform.gameObject);
        }
    }
    Image GetLetter(int index, Letter letter, ref Vector2 cursor)
    {
        var gameObject = new GameObject("Letter", typeof(RectTransform), typeof(Image));
        Image image = gameObject.GetComponent<Image>();
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(output.transform);
        rect.localScale = new Vector2(1 / rect.parent.lossyScale.x, 1 / rect.parent.lossyScale.y);

        rect.sizeDelta = Vector2.one * LetterSize;

        rect.anchoredPosition = cursor;
        letter.OnCreated(index, rect, ref cursor);
        return image;
    }

    // index is the index of the syllable in the word
    Vector2 GetPosition(int letterIndex, int index, int columns)
    {
        // should ONLY work for a lettercount of 2, replace if a dfiferent amount of letters is used
        // 1 - letterIndex % 2 will only ever be 1 or 0. Therefore the value can be plugged into the t parameter of
        // Mathf.Lerp to get -1 or 1.
        var yCoord = Mathf.Lerp(-1, 1, 1 - letterIndex % 2) * LetterHeightOffset;
        var minX = -LetterSize / 2 * (columns - 1);
        return new Vector2(minX + LetterSize * index, yCoord);
        // Index must be out of range if have not returned yet
        // throw new System.IndexOutOfRangeException("Parameter index greater than parameter columns");
    }

    Word tWord;
    Word[] sentence;
    void GroupSyllables()
    {
        tWord = new Word(engWord.Length); // Create a new Word object.
        for (int i = 0; i < engWord.Length; i++) // Traverse through the word.
        {
            tWord.AddLetter(engWord[i].ToString()); // Add an english symbol to the Word, taken from input. 
        }

        var check = "uu";
        for (int i = 0; i < tWord.letterLength; i++)
        {
            print("Sub: " + tWord[i, System.Math.Min(tWord.letterLength, i + check.Length)]);
            print("Match: " + tWord.Match(i, check));
            if (tWord.Match(i, check))
            {
                for (int j = i; j < i + check.Length; j++)
                {
                    tWord[j] = null;
                }
                print("old length: " + tWord.letterLength);
                tWord.letterLength += Letters[check].symbol.Length - check.Length; // Gets the change in length between the old symbol and the new one
                print("new length: " + tWord.letterLength + " w/ syllables " + tWord.syllables.ArrToString());
                tWord[i] = check;
                tWord.GroupSyllables();
                print("New: " + tWord.syllables.ArrToString());
            }
        }
        tWord.Clean();
        print("Last: " + tWord.syllables.ArrToString());
    }
    void PostParse()
    {
        tWord.TryAddLetter("e");
    }
    public void Reparse()
    {
        GroupSyllables();
        PostParse();
        print("Count: " + tWord.syllables.Count);
        print("Last: " + tWord.syllables.ArrToString());
        DestroyOldRectangles();
        OutputRectangles();
    }
}
