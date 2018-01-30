/**
 * Copyright (C) 2018 Regents of the University of California.
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
  /// NdnfsFile is a handler which fetches the %C1.FS.file file marker packet 
  /// for a version of a file served by NDNFS, and then assembles the contents 
  /// of segment packets into a single block of memory.
  /// </summary>
  public class NdnfsFile {
    public delegate void OnVersionContentSet
      (NdnfsFile ndnfsFile, Namespace contentNamespace, Blob content);

    /// <summary>
    /// Create an NdnfsFile object to assemble the contents of segment packets 
    /// into a single block of memory for the child version node. You should use 
    /// nameSpace.addOnContentSet to add the callback which is called when the 
    /// segmented content is complete for a child version node. After creating t
    /// his, you should call start().
    /// </summary>
    /// <param name="nameSpace">The Namespace node whose children are the names
    /// of versions of the file.</param>
    /// <param name="onVersionContentSet">(optional) When the segments are
    /// assembled for the file version, this calls 
    /// onVersionContentSet(ndnfsFile, contentNamespace, content) where
    /// ndnfsFile is this object, contentNamespace is the Namespace node for
    /// the file version, and content is the assembled content Blob (which is
    /// the same as contentNamespace.getContent()). If omitted, don't call.</param>
    public NdnfsFile
      (Namespace nameSpace, OnVersionContentSet onVersionContentSet = null)
    {
      namespace_ = nameSpace;
      onVersionContentSet_ = onVersionContentSet;

      namespace_.addOnContentSet(onContentSet);
    }

    /// <summary>
    /// Get the Namespace object for this handler.
    /// </summary>
    /// <returns>The Namespace object for this handler.</returns>
    public Namespace
    getNamespace() { return namespace_; }

    /// <summary>
    /// Start the process to fetch the %C1.FS.file marker packet and assemble 
    /// the segments.
    /// </summary>
    public void
    start() 
    {
      namespace_[fileMarker_].expressInterest();
    }

    private void
    onContentSet
      (Namespace nameSpace, Namespace contentNamespace, long callbackId)
    {
      if (contentNamespace.getName().size() == namespace_.getName().size() + 2 &&
          contentNamespace.getName()[-2].equals(fileMarker_)) {
        var version = contentNamespace.getName()[-1];
        if (version.isVersion()) {
          // Got the file marker for the version such as 
          // <namespace>/%C1.FS.file/<version>. Fetch the segmented content.
          var segmentedContent = new SegmentedContent(namespace_[version]);
          segmentedContent.start();
        }
      }
      else if (contentNamespace.getName().size() == namespace_.getName().size() + 1 &&
               contentNamespace.getName()[-1].isVersion()) {
        // We got the assembled content from the SegmentedContent handler.
        if (onVersionContentSet_ != null)
          onVersionContentSet_(this, contentNamespace, (Blob)contentNamespace.getContent());
      }
    }

    private Namespace namespace_;
    private OnVersionContentSet onVersionContentSet_;
    private static Name.Component fileMarker_ = new Name.Component
      (Name.fromEscapedString("%C1.FS.file"));
  }
}
