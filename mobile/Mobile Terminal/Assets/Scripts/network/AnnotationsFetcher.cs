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
//#define ENABLE_LOG

using System;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;
using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.cnl_dot_net;
using net.named_data.cnl_dot_net.usersync;

public delegate void FrameAnnotationsHandler(string jsonArrayString);

public class AnnotationsFetcher : ILogComponent
{
	private string serviceInstance_;
	private string servicePrefix_;
	private Namespace serviceNamespace;
	private FaceProcessor faceProcessor_;

	public AnnotationsFetcher (FaceProcessor faceProcessor, string servicePrefix, string instance)
	{
		faceProcessor_ = faceProcessor;
		serviceInstance_ = instance;
		serviceNamespace = new Namespace (new Name (servicePrefix));
		serviceNamespace.setFace(faceProcessor.getFace());
	}

	~AnnotationsFetcher(){
	}

    public string getServiceName() {
        return serviceInstance_;
    }

	public void fetchAnnotation(int frameNo, FrameAnnotationsHandler onAnnotationsFetched)
	{
        Namespace frameAnnotations = serviceNamespace
            .getChild(serviceInstance_)
            .getChild(Name.Component.fromSequenceNumber(frameNo));

        Debug.LogFormat (this, "spawned fetching for {0}", frameAnnotations.getName ().toUri ());

		frameAnnotations.addOnContentSet(delegate(Namespace nameSpace, Namespace contentNamespace, long callbackId) {

			if (contentNamespace.getName()[-1].toEscapedString() == "_meta") {
				var contentMetaInfo = (ContentMetaInfo)contentNamespace.getContent();

                Debug.LogFormat(this, "got meta info {0} {1}", contentMetaInfo.getContentType(),
                          contentMetaInfo.getOther().toString());

				if (!contentMetaInfo.getHasSegments())
					onAnnotationsFetched(contentMetaInfo.getOther().toString());
			}
			else if (contentNamespace == nameSpace) {
                Debug.LogFormat(this, "got segmented content size {0}", 
					((Blob)contentNamespace.getContent()).size());
				onAnnotationsFetched(contentNamespace.getContent().ToString());
			}
		});

		GeneralizedContent generalizedContent = new GeneralizedContent(frameAnnotations);
		generalizedContent.start();
	}

    public string getLogComponentName()
    {
        return "annotation-fetcher";
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

