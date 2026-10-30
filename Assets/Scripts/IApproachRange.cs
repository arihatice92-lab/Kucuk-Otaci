// Fındık'ın bir nesneye "yaklaştım" sayılması için gereken yatay mesafe (m).
// Ağaçtaki kedi gibi yaklaşılması zor nesneler daha geniş bir etkileşim alanı verebilir.
// Uygulamayan nesneler için CompanionController varsayılan değeri kullanır.
public interface IApproachRange
{
    float ApproachRadius { get; }
}