/**
 * Copyright (C) 2017-2018 Regents of the University of California.
 * @author: Jeff Thompson <jefft0@remap.ucla.edu>
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

using System;
using System.Collections.Generic;
using ILOG.J2CsMapping.NIO;
using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.cnl_dot_net.usersync;

namespace net.named_data.cnl_dot_net {
  /// <summary>
  /// GeneralizedContent is a handler which fetches the _meta packet for a
  /// generalized content and, if necessary, assembles the contents of segment 
  /// packets into a single block of memory.
  /// </summary>
  public class GeneralizedContent {
    /// <summary>
    /// Create a GeneralizedContent object to fetch the _meta packet for a
    /// generalized content and, if necessary, assemble the contents of segment 
    /// packets into a single block of memory. You should use 
    /// nameSpace.addOnContentSet to add the callback which is called when the 
    /// child _meta node has the ContentMetaInfo object and, if necessary, when
    /// the segmented content is complete. After creating this, you should call
    /// start().
    /// </summary>
    /// <param name="nameSpace">The Namespace node whose children are the names
    /// of segment Data packets. This is used to create a SegmentStream which
    /// you can access with getSegmentStream().</param>
    public GeneralizedContent(Namespace nameSpace)
    {
      namespace_ = nameSpace;
    }

    /// <summary>
    /// Get the Namespace object for this handler.
    /// </summary>
    /// <returns>The Namespace object for this handler.</returns>
    public Namespace
    getNamespace() { return namespace_; }

    /// <summary>
    /// Fetch the _meta packet and, if necessary, start fetching segment Data 
    /// packets. The library will call the callback given to 
    /// getNamespace().addOnContentSet .
    /// </summary>
    public void
    start() 
    {
      Namespace meta = namespace_["_meta"];
      // TODO: Use a way to set the callback which is better than setting the member.
      meta.transformContent_ = transformContentMetaInfo;
      meta.expressInterest();
    }

    /// <summary>
    /// This is called when a Data packet is received for the _meta child node.
    /// Decode and set the content as a ContentMetaInfo, then start fetching
    /// segments if necessary.
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="onContentTransformed">On content transformed.</param>
    private void
    transformContentMetaInfo(Data data, OnContentTransformed onContentTransformed)
    {
      var contentMetaInfo = new ContentMetaInfo();
      // TODO: Report errors decoding.
      contentMetaInfo.wireDecode(data.getContent());
      onContentTransformed(data, contentMetaInfo);

      if (contentMetaInfo.getHasSegments()) {
        // Start fetching segments.
        // TODO: Allow the caller to pass the SegmentStream in the constructor.
        SegmentedContent segmentedContent = new SegmentedContent(namespace_);
        segmentedContent.start();
      }
    }

    private Namespace namespace_;
  }
}
