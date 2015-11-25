using System.Threading.Tasks;

namespace Candy.Client.Models
{
    /// <summary>
    /// アプリケーションの状態を永続化するためのレポジトリ実装を定義します。
    /// </summary>
    public interface IStateRepository
    {
        // ApplicationManager と IStateRepository が循環参照してる…
        Task SaveAsync(ApplicationManager obj);
        Task LoadAsync(ApplicationManager obj);
    }
}