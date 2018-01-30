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
using net.named_data.jndn.util;

namespace net.named_data.cnl_dot_net {
  /// <summary>
  /// SegmentedContent assembles the contents of child segment packets into a
  /// single block of memory.
  /// </summary>
  public class SegmentedContent {
    /// <summary>
    /// Create a SegmentedContent object to use the given segmentStream to
    /// assemble content. You should use getNamespace().addOnContentSet to add
    /// the callback which is called when the content is complete. Then you
    /// should call start().
    /// </summary>
    /// <param name="segmentStream">The SegmentStream where the Namespace is a
    /// node whose children are the names of segment Data packets.</param>
    public SegmentedContent(SegmentStream segmentStream)
    {
      segmentStream_ = segmentStream;

      segmentStream_.addOnSegment(onSegment);
    }

    /// <summary>
    /// Create a SegmentedContent object to use a SegmentStream to assemble
    /// content. You should use nameSpace.addOnContentSet to add the callback
    /// which is called when the content is complete. Then you should call
    /// start().
    /// </summary>
    /// <param name="nameSpace">The Namespace node whose children are the names
    /// of segment Data packets. This is used to create a SegmentStream which
    /// you can access with getSegmentStream().</param>
    public SegmentedContent(Namespace nameSpace)
    : this (new SegmentStream(nameSpace))
    {
    }

    /// <summary>
    /// Get the SegmentStream given to the constructor or created in the
    /// constructor.
    /// </summary>
    /// <returns>The SegmentStream.</returns>
    public SegmentStream
    getSegmentStream() { return segmentStream_; }

    /// <summary>
    /// Get the Namespace object for this handler.
    /// </summary>
    /// <returns>The Namespace object for this handler.</returns>
    public Namespace
    getNamespace() { return segmentStream_.getNamespace(); }

    /// <summary>
    /// Start fetching segment Data packets. When done, the library will call
    /// the callback given to getNamespace().addOnContentSet .
    /// </summary>
    public void
    start() { segmentStream_.start(); }

    private void
    onSegment
      (SegmentStream segmentStream, Namespace segmentNamespace,
       long callbackId)
    {
      if (finished_)
        // We already finished and called onContent. (We don't expect this.)
        return;

      if (segmentNamespace != null) {
        segments_.Add((Blob)segmentNamespace.getContent());
        totalSize_ += ((Blob)segmentNamespace.getContent()).size();
      }
      else {
        // Finished. We don't need the callback anymore.
        segmentStream.removeCallback(callbackId);

        // Concatenate the segments.
        var content = ByteBuffer.wrap(new byte[totalSize_]);
        for (var i = 0; i < segments_.Count; ++i) {
          content.put(segments_[i].buf());
          // Free the memory.
          segments_[i] = new Blob();
        }
        content.flip();

        // Free memory.
        segments_.Clear();
        finished_ = true;

        // Debug: Fix this hack. How can we attach content to a namespace
        // node which has no associated Data packet? Who is authorized to do so?
        segmentStream_.getNamespace().debugOnContentTransformed
          (null, new Blob(content, false));
      }
    }

    private SegmentStream segmentStream_;
    private bool finished_ = false;
    private ArrayList<Blob> segments_ = new ArrayList<Blob>();
    private int totalSize_ = 0;
  }
}
