/**
 * Copyright (C) 2017 Regents of the University of California.
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
using System.Threading;
using System.Collections.Generic;

using net.named_data.jndn;
using net.named_data.jndn.util;
using net.named_data.cnl_dot_net;
using net.named_data.cnl_dot_net.usersync;

namespace TestCnlDotNet {
  /// <summary>
  /// This tests fetching generalized content which is the _meta packet and,
  /// if necessary, the segmented content.
  /// </summary>
  class TestSegmented {
    static void
    Main(string[] args)
    {
      Face face = new Face("localhost");
      Namespace prefix = new Namespace
        (new Name("/icear/user/peter/object_recognizer/%FE%01/yolo"));
      prefix.setFace(face);

      bool[] enabled = { true };
      prefix.addOnContentSet
        (delegate(Namespace nameSpace, Namespace contentNamespace, long callbackId) {
          onContentSet(nameSpace, contentNamespace, callbackId, enabled); });
      GeneralizedContent generalizedContent = new GeneralizedContent(prefix);
      generalizedContent.start();

      while (enabled[0]) {
        face.processEvents();
        // We need to sleep for a few milliseconds so we don't use 100% of the CPU.
        Thread.Sleep(10);
      }
    }

    /// <summary>
    /// This is called when the _meta child node is set and, if necessary, when
    /// the segments are reassembled.
    /// </summary>
    /// <param name="nameSpace">The calling Namespace.</param>
    /// <param name="contentNamespace">The Namespace where the content was set.
    /// </param>
    /// <param name="callbackId">The callback ID returned by addOnContentSet.
    /// </param>
    /// <param name="enabled">On success or error, set enabled[0] = false.
    /// </param>
    static void
    onContentSet
      (Namespace nameSpace, Namespace contentNamespace, long callbackId,
       bool[] enabled)
    {
      if (contentNamespace.getName()[-1].toEscapedString() == "_meta") {
        var contentMetaInfo = (ContentMetaInfo)contentNamespace.getContent();
        Console.Out.WriteLine
          ("Got meta info " + contentMetaInfo.getContentType() + " " +
           contentMetaInfo.getOther().toString());

        if (!contentMetaInfo.getHasSegments())
          // It will not fetch segments, so we are finished.
          enabled[0] = false;
      }
      else if (contentNamespace == nameSpace) {
        Console.Out.WriteLine
          ("Got segmented content size " + 
           ((Blob)contentNamespace.getContent()).size());
        enabled[0] = false;
      }
    }
  }
}
