/**
 * Copyright (C) 2017 Regents of the University of California.
 * @author: Peter Gusev <peter@remap.ucla.edu>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * A copy of the GNU Lesser General Public License is in the file COPYING.
 */
#define ENABLE_LOG
using System;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;
using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.cnl_dot_net;
using net.named_data.cnl_dot_net.usersync;

public delegate void AssetFetcherHandler ( AssetBundle assetBundle );

public class AssetBundleFetcher : ILogComponent  {

	private FaceProcessor faceProcessor_;

	public AssetBundleFetcher (FaceProcessor faceProcessor) {
		faceProcessor_ = faceProcessor;
	}

	public void fetch (string assetNdnUri, AssetFetcherHandler onAssetFetched) {
        Debug.LogFormat(this, "Will fetch asset {0}", assetNdnUri);
		
		var prefix =  new Namespace(assetNdnUri);
		prefix.setFace(faceProcessor_.getFace());
		
		var ndnfsFile = new NdnfsFile(prefix, delegate(NdnfsFile nf, Namespace contentNamespace, Blob content) {
            Debug.LogFormat(this, "got asset contents; size {0}", content.size());

			onAssetFetched(AssetBundle.LoadFromMemory(content.getImmutableArray()));
		});

		ndnfsFile.start();
	}

    public string getLogComponentName()
    {
        return "asset-fetcher";
    }

    public bool isLoggingEnabled()
    {
#if ENABLE_LOG
        return true;
#else
        return false;
#endif
    }
}
