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
using net.named_data.jndn;

namespace net.named_data.cnl_dot_net {
  /// <summary>
  /// SegmentStream attaches to a Namespace node to fetch and return child
  /// segment packets in order.
  /// </summary>
  public class SegmentStream {
    public delegate void OnSegment
      (SegmentStream segmentStream, Namespace segmentNamespace,
       long callbackId);

    /// <summary>
    /// Create a SegmentStream object to attach to the given namespace. You can
    /// add callbacks and set options, then you should call start().
    /// </summary>
    /// <param name="nameSpace">The Namespace node whose children are the names
    /// of segment Data packets.</param>
    public SegmentStream(Namespace nameSpace)
    {
      namespace_ = nameSpace;

      namespace_.addOnContentSet(onContentSet);
    }

    /// <summary>
    /// Add an onSegment callback. When a new segment is available, this calls
    /// onSegment as described below. Segments are supplied in order.
    /// </summary>
    /// <param name="onSegment">This calls
    /// onSegment(segmentStream, segmentNamespace, callbackId) where
    /// segmentStream is this SegmentStream, segmentNamespace is the Namespace
    /// where you can use segmentNamespace.getContent(), and callbackId is the
    /// callback ID returned by this method. You must check if segmentNamespace
    /// is null because after supplying the final segment, this calls
    /// onSegment(stream, null, callbackId) to signal the "end of stream".
    /// NOTE: The library will log any exceptions thrown by this callback, but
    /// for better error handling the callback should catch and properly handle
    /// any exceptions.</param>
    /// <returns>The callback ID which you can use in removeCallback().</returns>
    public long
    addOnSegment(OnSegment onSegment)
    {
      var callbackId = Namespace.getNextCallbackId();
      onSegmentCallbacks_[callbackId] = onSegment;
      return callbackId;
    }

    /// <summary>
    /// Remove the callback with the given callbackId. This does not search for
    /// the callbackId in child nodes. If the callbackId isn't found, do nothing.
    /// </summary>
    /// <param name="callbackId">The callback ID returned, for example, from
    /// addOnSegment.</param>
    public void
    removeCallback(long callbackId)
    {
      onSegmentCallbacks_.Remove(callbackId);
    }

    /// <summary>
    /// Get the Namespace object given to the constructor.
    /// </summary>
    /// <returns>The Namespace object given to the constructor.</returns>
    public Namespace
    getNamespace() { return namespace_; }

    /// <summary>
    /// Get the number of outstanding interests which this maintains while
    /// fetching segments.
    /// </summary>
    /// <returns>The Interest pipeline size.</returns>
    public int
    getInterestPipelineSize() { return interestPipelineSize_; }

    /// <summary>
    /// Set the number of outstanding interests which this maintains while
    /// fetching segments.
    /// </summary>
    /// <param name="interestPipelineSize">The Interest pipeline size.</param>
    /// <exception cref="Exception">If interestPipelineSize is less than 1.
    /// </exception>
    public void
    setInterestPipelineSize(int interestPipelineSize)
    {
      if (interestPipelineSize < 1)
        throw new Exception("The interestPipelineSize must be at least 1");
      interestPipelineSize_ = interestPipelineSize;
    }

    /// <summary>
    /// Start fetching segment Data packets and adding them as children of
    /// getNamespace(), calling any onSegment callbacks in order as the segments
    /// are received. Even though the segments supplied to onSegment are in
    /// order, note that children of the Namespace node are not necessarily
    /// added in order.
    /// </summary>
    /// <param name="interestCount">(optional) The number of initial Interests 
    /// to send for segments. By default this just sends an Interest for the 
    /// first segment and waits for the response before fetching more segments, 
    /// but if you know the number of segments you can reduce latency by 
    /// initially requesting more segments. (However, you should not use a 
    /// number larger than the Interest pipeline size.) If omitted, use 1.</param>
    public void
    start(int interestCount = 1) { requestNewSegments(interestCount); }

    /// <summary>
    /// Get the rightmost leaf of the given namespace. Use this temporarily to
    /// handle encrypted data packets where the name has the key name appended.
    /// </summary>
    /// <param name="nameSpace">The Namespace with the leaf node.</param>
    /// <returns>The leaf Namespace node.</returns>
    private static Namespace
    debugGetRightmostLeaf(Namespace nameSpace)
    {
      var result = nameSpace;
      while (true) {
        var childComponents = result.getChildComponents();
        if (childComponents.Count == 0)
          return result;

        result = result.getChild(childComponents[childComponents.Count - 1]);
      }
    }

    private void
    onContentSet
      (Namespace nameSpace, Namespace contentNamespace, long callbackId)
    {
      if (!(contentNamespace.getName().size() >= namespace_.getName().size() + 1 &&
        contentNamespace.getName()[namespace_.getName().size()].isSegment()))
        // Not a segment, ignore.
        return;

      // TODO: Use the Namespace mechanism to validate the Data packet.

      var metaInfo = contentNamespace.getData().getMetaInfo();
      if (metaInfo.getFinalBlockId().getValue().size() > 0 &&
          metaInfo.getFinalBlockId().isSegment())
        finalSegmentNumber_ = metaInfo.getFinalBlockId().toSegment();

      // Report as many segments as possible where the node already has content.
      while (true) {
        var nextSegmentNumber = maxRetrievedSegmentNumber_ + 1;
        var nextSegment = debugGetRightmostLeaf
          (namespace_[Name.Component.fromSegment(nextSegmentNumber)]);
        if (nextSegment.getContent() == null)
          break;

        maxRetrievedSegmentNumber_ = nextSegmentNumber;
        fireOnSegment(nextSegment);

        if (finalSegmentNumber_ >= 0 && nextSegmentNumber == finalSegmentNumber_) {
          // Finished.
          fireOnSegment(null);
          return;
        }
      }

      if (finalSegmentNumber_ < 0 && !didRequestFinalSegment_) {
        didRequestFinalSegment_ = true;
        // Try to determine the final segment now.
        var interestTemplate = new Interest();
        interestTemplate.setInterestLifetimeMilliseconds(4000.0);
        interestTemplate.setChildSelector(1);
        namespace_.expressInterest(interestTemplate);
      }

      requestNewSegments(interestPipelineSize_);
    }

    private void
    requestNewSegments(int maxRequestedSegments)
    {
      if (maxRequestedSegments < 1)
        maxRequestedSegments = 1;

      var childComponents = namespace_.getChildComponents();
      // First, count how many are already requested and not received.
      var nRequestedSegments = 0;
      foreach (var component in childComponents) {
        if (!component.isSegment())
          // The namespace contains a child other than a segment. Ignore.
          continue;

        var child = namespace_[component];
        // Debug: Check the leaf for content, but use the immediate child
        // for _debugSegmentStreamDidExpressInterest.
        if (debugGetRightmostLeaf(child).getContent() == null &&
            child.debugSegmentStreamDidExpressInterest_) {
          ++nRequestedSegments;
          if (nRequestedSegments >= maxRequestedSegments)
            // Already maxed out on requests.
            break;
        }
      }

      // Now find unrequested segment numbers and request.
      var segmentNumber = maxRetrievedSegmentNumber_;
      while (nRequestedSegments < maxRequestedSegments) {
        ++segmentNumber;
        if (finalSegmentNumber_ >= 0 && segmentNumber > finalSegmentNumber_)
          break;

        var segment = namespace_[Name.Component.fromSegment(segmentNumber)];
        if (debugGetRightmostLeaf(segment).getContent() != null ||
          segment.debugSegmentStreamDidExpressInterest_)
          // Already got the data packet or already requested.
          continue;

        ++nRequestedSegments;
        segment.debugSegmentStreamDidExpressInterest_ = true;
        segment.expressInterest();
      }
    }

    private void
    fireOnSegment(Namespace segmentNamespace)
    {
      // Copy the keys before iterating since callbacks can change the list.
      var keys = new long[onSegmentCallbacks_.Count];
      onSegmentCallbacks_.Keys.CopyTo(keys, 0);

      foreach (long key in keys) {
        // A callback on a previous pass may have removed this callback, so check.
        OnSegment onSegment;
        if (onSegmentCallbacks_.TryGetValue(key, out onSegment)) {
          // TODO: Log exceptions.
          onSegment(this, segmentNamespace, key);
        }
      }
    }

    private Namespace namespace_;
    private long maxRetrievedSegmentNumber_ = -1;
    private bool didRequestFinalSegment_ = false;
    private long finalSegmentNumber_ = -1;
    private int interestPipelineSize_ = 8;
    // The key is the callback ID. The value is the OnSegment function.
    private Dictionary<long, OnSegment> onSegmentCallbacks_ =
      new Dictionary<long, OnSegment>();
  }
}
