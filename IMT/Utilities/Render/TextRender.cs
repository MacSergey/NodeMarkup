using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ColossalFramework.UI.UIDynamicFont;

namespace IMT.Utilities
{
    public static class TextRenderHelper
    {
        public static string[] InstalledFonts { get; } = Font.GetOSInstalledFontNames();

        public class TextRenderer
        {
            private static UIDynamicFont DefaultFont { get; }
            private static Dictionary<string, UIDynamicFont> Fonts { get; } = new Dictionary<string, UIDynamicFont>();

            public UIDynamicFont Font { get; }
            public float TextScale { get; set; }
            public Vector2 Spacing { get; set; } = Vector2.one;
            public int TabSize { get; set; }
            public bool WordWrap { get; set; }

            private float CharSpacing => Spacing.x * TextScale;
            private float LineSpacing => Spacing.y * TextScale;

            static TextRenderer()
            {
                var view = UIView.GetAView();

                DefaultFont = ScriptableObject.CreateInstance<UIDynamicFont>();
                DefaultFont.baseFont = (view.defaultFont as UIDynamicFont).baseFont;
                DefaultFont.material = DefaultFont.baseFont.material;
                DefaultFont.size = DefaultFont.baseFont.fontSize;
                DefaultFont.baseline = 18;
                DefaultFont.lineHeight = 22;

                //var baseFont = new Font(font.name);
                //baseFont.fontNames = font.baseFont.fontNames;
                //baseFont.material = new Material(Shader.Find("UI/Dynamic Font Shader"));
                //baseFont.material.renderQueue = 4000;
                //baseFont.material.name = "IMT font";
                //baseFont.material.mainTexture = TextureHelper.CreateTexture(256, 256, new Color(0, 0, 0, 0));
                //DefaultFont = ScriptableObject.CreateInstance<UIDynamicFont>();
                //DefaultFont.baseFont = baseFont;
                //DefaultFont.baseline = font.baseline;

            }
            public TextRenderer(string fontName)
            {
                if (!string.IsNullOrEmpty(fontName))
                {
                    if (!Fonts.TryGetValue(fontName, out var font))
                    {
                        font = ScriptableObject.CreateInstance<UIDynamicFont>();
                        font.baseFont = UnityEngine.Font.CreateDynamicFontFromOSFont(fontName, 16);
                        font.material = font.baseFont.material;
                        font.size = font.baseFont.fontSize;
                        font.baseline = 18;
                        font.lineHeight = 22;
                        Fonts.Add(fontName, font);
                    }

                    if (font.isValid)
                    {
                        Font = font;
                        return;
                    }
                }

                Font = DefaultFont;
            }

            public Texture2D Render(string text, out float textWidth, out float textHeight)
            {
                var tokens = Tokenize(text);
                foreach (var token in tokens)
                    CalculateTokenRenderSize(token);

                var lineTokens = CalculateLineBreaks(tokens);

                textWidth = lineTokens.Count > 0 ? lineTokens.Max(l => l.Width) : 0;
                textHeight = lineTokens.Sum(l => l.Height) + (lineTokens.Count - 1) * LineSpacing;

                var texture = new Texture2D(Get2Pow(Mathf.CeilToInt(textWidth)), Get2Pow(Mathf.CeilToInt(textHeight)))
                {
                    name = "Text",
                };
                var pixels = texture.GetPixels();
                for (var i = 0; i < pixels.Length; i += 1)
                    pixels[i] = Color.red;
                texture.SetPixels(pixels);
                //#if DEBUG
                //                for (var i = 0; i < texture.width; i += 1)
                //                {
                //                    texture.SetPixel(i, 0, Color.black);
                //                    texture.SetPixel(i, texture.height - 1, Color.black);
                //                }
                //                for (var i = 0; i < texture.height; i += 1)
                //                {
                //                    texture.SetPixel(0, i, Color.black);
                //                    texture.SetPixel(texture.width - 1, 0, Color.black);
                //                }
                //#endif
                var fontTexture = Font.texture.MakeReadable();
                var position = new Vector2(0f, (texture.height - textHeight) * 0.5f);
                for (int i = lineTokens.Count - 1; i >= 0; i -= 1)
                {
                    var line = lineTokens[i];
                    RenderLine(texture, position, line, tokens, fontTexture);
                    position.y += line.Height + LineSpacing;
                }

                texture.Apply();
                return texture;
            }
            private void RenderLine(Texture2D texture, Vector2 position, LineToken line, List<Token> tokens, Texture2D fontTexture)
            {
                //#if DEBUG
                //                for (var i = 0; i < texture.width; i += 1)
                //                {
                //                    texture.SetPixel(i, Mathf.CeilToInt(position.y), Color.black);
                //                    texture.SetPixel(i, Mathf.CeilToInt(position.y + line.Height), Color.black);
                //                }
                //#endif
                position.x = (texture.width - line.Width) * 0.5f;
                for (var i = line.StartIndex; i <= line.EndIndex; i += 1)
                {
                    var token = tokens[i];
                    RenderToken(texture, position, token, fontTexture);
                    position.x += token.Width;
                }
            }

            private void RenderToken(Texture2D texture, Vector2 position, Token token, Texture2D fontTexture)
            {
                var text = token.Text;
                var size = Mathf.CeilToInt(Font.size * TextScale);

                for (var i = 0; i < text.Length; i += 1)
                {
                    if (i > 0)
                        position.x += CharSpacing;

                    if (!Font.baseFont.GetCharacterInfo(text[i], out var info, size))
                        continue;

                    var minX = Mathf.CeilToInt(position.x + info.minX);
                    var maxX = minX + info.glyphWidth;

                    var minY = Mathf.CeilToInt(position.y - info.glyphHeight + info.maxY + Font.Descent * TextScale);
                    var maxY = minY + info.glyphHeight;

                    var uvMin = new Vector2(info.uvTopLeft.x * fontTexture.width, info.uvTopLeft.y * fontTexture.height);

                    if (token.Type == TokenType.Text)
                    {
                        var deltaX = maxX - minX;
                        var deltaY = maxY - minY;
                        var uvDeltaX = new Vector2((info.uvTopRight.x - info.uvTopLeft.x) * fontTexture.width, (info.uvTopRight.y - info.uvTopLeft.y) * fontTexture.height);
                        var uvDeltaY = new Vector2((info.uvBottomLeft.x - info.uvTopLeft.x) * fontTexture.width, (info.uvBottomLeft.y - info.uvTopLeft.y) * fontTexture.height);

                        for (int xi = 0; xi < deltaX; xi += 1)
                        {
                            var x = minX + xi;
                            for (int yi = 0; yi < deltaY; yi += 1)
                            {
                                var y = maxY - yi;
                                var uv = uvMin + uvDeltaX / deltaX * xi + uvDeltaY / deltaY * yi;
                                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                                {
                                    var color = fontTexture.GetPixel((int)uv.x, (int)uv.y);
                                    if (color.a > 0f)
                                        texture.SetPixel(x, y, new Color(1f - color.a, 0f, 0f, 1f));
                                }
                            }
                        }
                    }

                    position.x += Mathf.CeilToInt(info.maxX);
                }
            }

            private List<Token> Tokenize(string text)
            {
                List<Token> tokens = new List<Token>();

                int i = 0;
                int num = 0;
                int length = text.Length;
                while (i < length)
                {
                    if (text[i] == '\r')
                    {
                        i += 1;
                        num = i;
                        continue;
                    }

                    for (; i < length && !char.IsWhiteSpace(text[i]); i += 1)
                    {
                        if (parsingType == ParsingType.Chinese && text[i].IsChinese())
                        {
                            i += 1;
                            break;
                        }
                    }

                    if (i > num)
                    {
                        tokens.Add(new Token(text, TokenType.Text, num, i - 1));
                        num = i;
                    }

                    if (i < length && text[i] == '\n')
                    {
                        tokens.Add(new Token(text, TokenType.Newline, i, i));
                        i += 1;
                        num = i;
                    }

                    while (i < length && text[i] != '\n' && text[i] != '\r' && char.IsWhiteSpace(text[i]))
                    {
                        i += 1;
                    }

                    if (i > num)
                    {
                        tokens.Add(new Token(text, TokenType.Whitespace, num, i - 1));
                        num = i;
                    }
                }

                return tokens;
            }
            private void CalculateTokenRenderSize(Token token)
            {
                var size = Mathf.CeilToInt(Font.size * TextScale);
                Font.RequestCharacters(token.Text, size, FontStyle.Normal);

                var width = 0f;
                if (token.Type == TokenType.Text)
                {

                    for (var i = 0; i < token.Length; i++)
                    {
                        var c = token[i];
                        Font.baseFont.GetCharacterInfo(c, out var info, size, FontStyle.Normal);

                        switch (c)
                        {
                            case '\t':
                                width += TabSize;
                                break;
                            case ' ':
                                width += info.advance + CharSpacing;
                                break;
                            default:
                                width += info.maxX;
                                break;
                        }
                    }

                    if (token.Length > 2)
                        width += (token.Length - 1) * CharSpacing;
                }
                else if (token.Type == TokenType.Whitespace)
                {
                    for (var i = 0; i < token.Length; i++)
                    {
                        var c = token[i];
                        switch (c)
                        {
                            case '\t':
                                width += TabSize;
                                break;
                            case ' ':
                                Font.baseFont.GetCharacterInfo(c, out var info, size, FontStyle.Normal);
                                width += info.advance + CharSpacing;
                                break;
                        }
                    }
                }

                token.Height = Mathf.CeilToInt(Font.lineHeight * TextScale);
                token.Width = Mathf.CeilToInt(width);
            }
            private List<LineToken> CalculateLineBreaks(List<Token> tokens)
            {
                var lines = new List<LineToken>();

                var index = 0;
                var startIndex = 0;
                var nextIndex = 0;
                var lineWidth = 0;

                while (nextIndex < tokens.Count)
                {
                    var token = tokens[nextIndex];
                    TokenType tokenType = token.Type;

                    if (tokenType == TokenType.Newline)
                    {
                        lines.Add(new LineToken(tokens, startIndex, nextIndex));
                        startIndex = (index = ++nextIndex);
                        lineWidth = 0;
                        continue;
                    }

                    var tokenWidth = Mathf.CeilToInt(token.Width);
                    if (WordWrap && index > startIndex && tokenType == TokenType.Text)
                    {
                        if (index > startIndex)
                        {
                            if (parsingType == ParsingType.Chinese)
                                lines.Add(new LineToken(tokens, startIndex, index));
                            else
                                lines.Add(new LineToken(tokens, startIndex, index - 1));

                            startIndex = (nextIndex = ++index);
                            lineWidth = 0;
                        }
                        else
                        {
                            lines.Add(new LineToken(tokens, startIndex, index - 1));
                            index = ++nextIndex;
                            startIndex = index;
                            lineWidth = 0;
                        }
                    }
                    else
                    {
                        if (tokenType == TokenType.Whitespace)
                            index = nextIndex;
                        else if (parsingType == ParsingType.Chinese && tokenType == TokenType.Text && token.Length == 1 && token[0].IsChinese())
                            index = nextIndex;

                        lineWidth += tokenWidth;
                        nextIndex++;
                    }
                }

                if (startIndex < tokens.Count)
                    lines.Add(new LineToken(tokens, startIndex, tokens.Count - 1));

                for (int i = 0; i < lines.Count; i += 1)
                {
                    var line = lines[i];

                    line.Height = Font.baseline * TextScale;
                    line.Width = 0;

                    for (var j = line.StartIndex; j <= line.EndIndex; j += 1)
                        line.Width += tokens[j].Width;
                }

                return lines;
            }
            private int Get2Pow(int length)
            {
                if (length >= 1024)
                    return 1024;
                else if (length <= 32)
                    return 32;
                else
                {
                    var i = 10;

                    while (i > 5 && ((1 << i) & length) == 0)
                    {
                        i -= 1;
                    }

                    return 1 << (i + 1);
                }
            }
        }

        private class Token
        {
            public string Source { get; }
            public TokenType Type { get; }
            public int StartIndex { get; }
            public int EndIndex { get; }
            public int Width { get; set; }
            public int Height { get; set; }

            private string _text;
            public string Text
            {
                get
                {
                    if (_text == null)
                    {
                        int num = Math.Min(EndIndex - StartIndex + 1, Source.Length - StartIndex);
                        _text = Source.Substring(StartIndex, num);
                    }

                    return _text;
                }
            }
            public int Length => EndIndex - StartIndex + 1;
            public char this[int index]
            {
                get
                {
                    if (index < 0 || index >= Length)
                        throw new IndexOutOfRangeException(string.Format("Index {0} is out of range ({2}:{1})", index, Length, Text));
                    else
                        return Source[StartIndex + index];
                }
            }

            public Token(string source, TokenType type, int startIndex, int endIndex)
            {
                Source = source;
                Type = type;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            public override string ToString()
            {
                if (Type == TokenType.Text)
                    return $"{Type}: \"{Text}\"";
                else
                    return Type.ToString();
            }
        }
        private class LineToken
        {
            public List<Token> Tokens { get; }
            public int StartIndex { get; }
            public int EndIndex { get; }
            public float Width { get; set; }
            public float Height { get; set; }

            public int Length => EndIndex - StartIndex + 1;

            public LineToken(List<Token> tokens, int startIndex, int endIndex)
            {
                Tokens = tokens;
                StartIndex = startIndex;
                EndIndex = endIndex;
            }
            public override string ToString()
            {
                return $"{Length} Tokens";
            }
        }
        private enum TokenType
        {
            Invalid,
            Text,
            Whitespace,
            Newline,
        }
    }
}

