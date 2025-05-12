using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AnimLite
{
    using AnimLite.Utility;
    using AnimLite.Vmd;
    using AnimLite.Vrm;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;

    public class PrototypeCacheManager : MonoBehaviour
    {

        public bool UseVmdCache = true;
        public bool UseModelCache = true;


        public PrototypeCacheHolder Holder;


        private void OnEnable()
        {
            this.Holder = new(this.UseVmdCache, this.UseModelCache);
        }

        async ValueTask OnDisable()
        {
            await this.Holder.DoIfNotNullAsync(x => x.DisposeAsync());
        }
    }




}
