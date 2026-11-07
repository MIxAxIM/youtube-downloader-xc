using System.Collections.Generic;
using System.Linq;

namespace YoutubeDownloader.XC.Core.Downloading;

public readonly record struct QueueCardState(
    bool HasStarted,
    bool IsAudio,
    string Container,
    int? BitrateKbps,
    int? SelectedHeight
);

public static class QueueDefaults
{
    public static QueueCardState Apply(
        QueueCardState card,
        bool isAudio,
        string container,
        int? bitrateKbps
    )
    {
        if (card.HasStarted)
            return card;

        return card with
        {
            IsAudio = isAudio,
            Container = container,
            BitrateKbps = isAudio ? bitrateKbps : null,
        };
    }

    public static IReadOnlyList<QueueCardState> ApplyAll(
        IReadOnlyList<QueueCardState> cards,
        bool isAudio,
        string container,
        int? bitrateKbps
    ) => cards.Select(card => Apply(card, isAudio, container, bitrateKbps)).ToArray();
}
