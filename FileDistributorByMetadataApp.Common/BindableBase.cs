using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace FileDistributorByMetadataApp.Common
{
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged = null!;

        public void OnPropertyChanged<TProperty>(Expression<Func<TProperty>> property)
        {
            if (property.Body is MemberExpression expression)
                OnPropertyChanged(expression.Member.Name);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}