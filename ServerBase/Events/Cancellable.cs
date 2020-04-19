using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Cancellable
{
    private bool isCancelled = false;
    public bool IsCancelled => isCancelled;

    public void CancelIf(bool value)
    {
        if (value)
        {
            isCancelled = true;
        }
    }

    public void Cancel()
    {
        isCancelled = true;
    }

    public static bool CheckCancelled(LocalEvent<Cancellable> evt) {
        var cancellable = new Cancellable();
        evt.Fire(cancellable);
        return cancellable.IsCancelled;
    }
}