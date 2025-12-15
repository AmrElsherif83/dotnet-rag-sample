namespace RAG.Core.Chunking;

/// <summary>
/// Provides methods for splitting text into chunks for processing.
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Splits text into chunks of approximately the specified character size with overlap.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkSize">The approximate size of each chunk in characters.</param>
    /// <param name="overlap">The number of overlapping characters between consecutive chunks.</param>
    /// <returns>A read-only list of text chunks.</returns>
    public static IReadOnlyList<string> ChunkByCharacters(string text, int chunkSize = 500, int overlap = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");
        }

        if (overlap < 0 || overlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be non-negative and less than chunk size.");
        }

        var chunks = new List<string>();
        var position = 0;
        var textLength = text.Length;

        while (position < textLength)
        {
            var remainingLength = textLength - position;
            var currentChunkSize = Math.Min(chunkSize, remainingLength);
            var chunk = text.Substring(position, currentChunkSize);
            chunks.Add(chunk);

            // Move position forward by (chunkSize - overlap)
            position += chunkSize - overlap;

            // If the remaining text is smaller than overlap, we're done
            if (position >= textLength)
            {
                break;
            }
        }

        return chunks;
    }

    /// <summary>
    /// Splits text into chunks respecting paragraph boundaries (double newlines).
    /// Merges small paragraphs and splits large ones to approach the maximum chunk size.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="maxChunkSize">The maximum size of each chunk in characters.</param>
    /// <returns>A read-only list of text chunks.</returns>
    public static IReadOnlyList<string> ChunkByParagraphs(string text, int maxChunkSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        if (maxChunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Max chunk size must be greater than zero.");
        }

        var chunks = new List<string>();
        
        // Split by double newlines to get paragraphs
        var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var paragraph in paragraphs)
        {
            var trimmedParagraph = paragraph.Trim();
            if (string.IsNullOrEmpty(trimmedParagraph))
            {
                continue;
            }

            var paragraphLength = trimmedParagraph.Length;

            // If this single paragraph is larger than maxChunkSize, split it
            if (paragraphLength > maxChunkSize)
            {
                // First, add any accumulated chunks
                if (currentChunk.Count > 0)
                {
                    chunks.Add(string.Join("\n\n", currentChunk));
                    currentChunk.Clear();
                    currentLength = 0;
                }

                // Split the large paragraph by sentences or characters
                var largeParagraphChunks = ChunkByCharacters(trimmedParagraph, maxChunkSize, maxChunkSize / 10);
                chunks.AddRange(largeParagraphChunks);
            }
            // If adding this paragraph would exceed maxChunkSize, start a new chunk
            else if (currentLength + paragraphLength + (currentChunk.Count > 0 ? 2 : 0) > maxChunkSize && currentChunk.Count > 0)
            {
                chunks.Add(string.Join("\n\n", currentChunk));
                currentChunk.Clear();
                currentChunk.Add(trimmedParagraph);
                currentLength = paragraphLength;
            }
            // Otherwise, add to current chunk
            else
            {
                currentChunk.Add(trimmedParagraph);
                currentLength += paragraphLength + (currentChunk.Count > 1 ? 2 : 0); // +2 for "\n\n"
            }
        }

        // Add any remaining chunk
        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join("\n\n", currentChunk));
        }

        return chunks;
    }
}
