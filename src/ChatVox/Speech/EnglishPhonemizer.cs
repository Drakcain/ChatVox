using System.Globalization;
using System.Text;
using DotNetG2P.English;
using KokoroSharp.Processing;
using MisakiSharp;

namespace ChatVox.Speech;

public sealed record EnglishPhonemization(string NormalizedText, int[] Tokens)
{
    public bool HasSpeech => Tokens.Length > 0;
}

public static class EnglishPhonemizer
{
    private static readonly Lazy<EnglishG2PEngine> OutOfVocabularyEnglish = new(() => new EnglishG2PEngine());
    private static readonly Lazy<EnglishG2P> American = new(() => new EnglishG2P(EnglishG2P.DefaultTagger, british: false, espeakFallback: EstimateOutOfVocabularyPhonemes));
    private static readonly Lazy<EnglishG2P> British = new(() => new EnglishG2P(EnglishG2P.DefaultTagger, british: true, espeakFallback: EstimateOutOfVocabularyPhonemes));

    public static EnglishPhonemization Phonemize(string? text, bool british)
    {
        var normalized = Normalize(text);
        if (normalized.Length == 0) return new(string.Empty, []);

        var phonemes = (british ? British.Value : American.Value).Phonemize(normalized).Phonemes;
        var tokens = phonemes.Where(Tokenizer.Vocab.ContainsKey).Select(phoneme => Tokenizer.Vocab[phoneme]).ToArray();
        return new(normalized, tokens);
    }

    private static string EstimateOutOfVocabularyPhonemes(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            // Apache-2.0, managed CMU/Flite LTS fallback: preserves natural
            // username pronunciation without eSpeak or letter-by-letter speech.
            return OutOfVocabularyEnglish.Value.ToIPA(text);
        }
        catch
        {
            // A malformed token must never interrupt the chat speech worker.
            return string.Empty;
        }
    }

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(character switch
            {
                '\u2018' or '\u2019' or '\u201B' => '\'',
                '\u201C' or '\u201D' or '\u201F' => '"',
                '\u2013' or '\u2014' => '-',
                '\u00A0' => ' ',
                _ => character
            });
        }

        return string.Join(' ', builder.ToString().Normalize(NormalizationForm.FormC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
