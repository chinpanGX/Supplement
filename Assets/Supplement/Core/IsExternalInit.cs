/*
このクラスを定義することで、C# 9.0以降のレコード型やinit-onlyプロパティを使用可能にします。
*/

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IsExternalInit
    {
    }
}