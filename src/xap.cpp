/* -*- Mode: C++; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * xap.cpp:
 *
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 * 
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <glib.h>

#include <string.h>
#include <stdlib.h>

#include "zip/unzip.h"
#include "xaml.h"
#include "error.h"
#include "utils.h"
#include "type.h"
#include "xap.h"

char *
Xap::Unpack (const char *fname)
{
	unzFile zipfile;
	char *xap_dir;
	
	if (!(xap_dir = CreateTempDir (fname)))
		return NULL;
	
	if (!(zipfile = unzOpen (fname))) {
		RemoveDir (xap_dir);
		g_free (xap_dir);
		return NULL;
	}
	
	// FIXME: at some point we'll want to pass 'true' here (as part of the
	// fix for case-insensitive resource access)
	if (!ExtractAll (zipfile, xap_dir, false)) {
		RemoveDir (xap_dir);
		unzClose (zipfile);
		g_free (xap_dir);
		return NULL;
	}
	
	return xap_dir;
}

Xap::Xap (XamlLoader *loader, char *xap_dir, DependencyObject *root)
{
	this->loader = loader;
	this->xap_dir = xap_dir;
	this->root = root;
}

Xap::~Xap ()
{
	g_free (xap_dir);
	xap_dir = NULL;
}

Xap *
xap_create_from_file (XamlLoader *loader, const char *filename)
{
	char *xap_dir = Xap::Unpack (filename);
	Type::Kind element_type;
	DependencyObject *element;

	if (xap_dir == NULL)
		return NULL;

	// Load the AppManifest file
	char *manifest = g_build_filename (xap_dir, "AppManifest.xaml", NULL);
	element = loader->CreateDependencyObjectFromFile (manifest, false, &element_type);
	g_free (manifest);

	if (element_type != Type::DEPLOYMENT)
		return NULL;

	// TODO: Create a DependencyObject from the root node.

	Xap *xap = new Xap (loader, xap_dir, element);
	return xap;
}
