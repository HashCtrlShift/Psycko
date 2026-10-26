namespace Psycko.Core.Interfaces
{
    /// <summary>
    /// Vue volontairement minimale fournie à un agent pour choisir une carte FaceDown.
    /// Elle n'expose que le nombre de cartes : le contenu est inaccessible par construction
    /// (choix à l'aveugle, par position uniquement).
    /// </summary>
    public interface IFaceDownBlindView
    {
        /// <summary>Nombre de cartes FaceDown du joueur (toujours supérieur à 0 à l'appel).</summary>
        int SelfFaceDownCount { get; }
    }
}