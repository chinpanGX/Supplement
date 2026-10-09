using System;

namespace Supplement.Core
{
    /// <summary>
    /// 指定したIDのエンティティが見つからないときに投げる。
    /// </summary>
    public class EntityNotFoundException : Exception
    {
        /// <summary>
        /// メッセージを指定して作る。
        /// </summary>
        /// <param name="message">見つからなかった内容。</param>
        public EntityNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// エンティティの型名とIDから、メッセージを作る。
        /// </summary>
        /// <param name="entityTypeName">エンティティの型名。</param>
        /// <param name="id">見つからなかったID。</param>
        public EntityNotFoundException(string entityTypeName, string id)
            : base($"Entity of type {entityTypeName} with ID {id} was not found.")
        {
        }

        /// <summary>
        /// エンティティの型名とIDから、メッセージを作る。
        /// </summary>
        /// <param name="entityTypeName">エンティティの型名。</param>
        /// <param name="id">見つからなかったID。</param>
        public EntityNotFoundException(string entityTypeName, int id)
            : base($"Entity of type {entityTypeName} with ID {id} was not found.")
        {
        }

        /// <summary>
        /// エンティティの型名とIDから、メッセージを作る。
        /// </summary>
        /// <param name="entityTypeName">エンティティの型名。</param>
        /// <param name="id">見つからなかったID。</param>
        public EntityNotFoundException(string entityTypeName, long id)
            : base($"Entity of type {entityTypeName} with ID {id} was not found.")
        {
        }
    }
}