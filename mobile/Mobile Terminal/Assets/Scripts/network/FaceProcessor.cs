/**
 * Copyright (C) 2018 Regents of the University of California.
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

using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

using UnityEngine;
using net.named_data.jndn;
using net.named_data.jndn.util;

public class FaceProcessor  {
	private Face face_;
	private Thread faceThread_;
	private bool runThread_;

	public FaceProcessor() {
		face_ = new Face("localhost");
	}

	~FaceProcessor() {
		runThread_ = false;
		faceThread_.Join();
	}

	public void start() {
		runThread_ = true;
		faceThread_ = new Thread(new ThreadStart(delegate() {
			while (runThread_)
			{
				processFace();
			}
		}));

		faceThread_.Priority = System.Threading.ThreadPriority.Highest;
		faceThread_.Start ();
	}

	public void stop() {
		runThread_ = false;
		faceThread_.Join();
	}

	public Face getFace() {
		return face_;
	}

	private void processFace() {
		face_.processEvents();
	}
	
}
