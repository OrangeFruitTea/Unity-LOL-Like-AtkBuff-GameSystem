using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Basement.Json
{
    public interface IJsonStorage
    {
        void Save<T>(string key, T data);
        T Load<T>(string key);
        bool Exists(string key);
        void Delete(string key);
        void Clear();
        IEnumerable<string> GetAllKeys();
        
        Task SaveAsync<T>(string key, T data);
        Task<T> LoadAsync<T>(string key);
        Task<bool> ExistsAsync(string key);
        Task DeleteAsync(string key);
        Task ClearAsync();
    }
}
