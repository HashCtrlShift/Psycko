namespace Psycko.Core.Domain
{
    /// <summary>
    /// Raison métier du rejet d'un coup proposé.
    /// Un rejet est un cas normal (bot ou humain), jamais une exception.
    /// </summary>
    public enum PlayRejectionReason
    {
        /// <summary>Le joueur qui propose le coup n'est pas le joueur actif.</summary>
        NotYourTurn,

        /// <summary>Le joueur a déjà terminé la partie (Phase = Finished).</summary>
        PlayerFinished,

        /// <summary>La partie est déjà terminée.</summary>
        GameAlreadyOver,

        /// <summary>Cartes non jouables selon les Rules (utilisé à partir de T22.c).</summary>
        InvalidCards
    }
}