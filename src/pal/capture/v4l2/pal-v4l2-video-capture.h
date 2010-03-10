/* -*- Mode: C++; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * pal-video-capture-v4l2.h:
 *
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 * 
 */

#ifndef MOON_PAL_VIDEO_CAPTURE_V4L2_H
#define MOON_PAL_VIDEO_CAPTURE_V4L2_H

#include "pal.h"

class MoonVideoCaptureServiceV4L2 : public MoonVideoCaptureService {
public:
	MoonVideoCaptureServiceV4L2 ();
	virtual ~MoonVideoCaptureServiceV4L2 ();

	virtual MoonVideoCaptureDevice* GetDefaultCaptureDevice ();
	virtual List* GetAvailableCaptureDevices ();
};

#endif /* MOON_PAL_VIDEO_CAPTURE_V4L2_H */