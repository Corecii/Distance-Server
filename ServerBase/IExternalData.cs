using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


interface IExternalData
{
    T GetExternalData<T>();
    void AddExternalData(object val);
    void RemoveExternalData<T>();
}
