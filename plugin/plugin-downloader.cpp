/*
 * plugin-downloader.cpp: Moonlight plugin download routines.
 *
 * Author:
 *   Everaldo Canuto (everaldo@novell.com)
 *
 * Copyright 2007 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 *
 */

#include "plugin-downloader.h"
#include "browser-mmsh.h"

bool downloader_shutdown = false;

static gpointer
p_downloader_create_state (Downloader *dl)
{
  //	DEBUGMSG ("downloader_create_state");
	return new PluginDownloader (dl);
}

static void
p_downloader_destroy_state (gpointer data)
{
	delete (PluginDownloader *) data;
}

static void
p_downloader_open (const char *verb, const char *uri, gpointer state)
{
	PluginDownloader *pd = (PluginDownloader *) state;

	g_free (pd->verb);
	g_free (pd->uri);

	pd->verb = g_strdup (verb);
	pd->uri = g_strdup (uri);
	if (strncmp (uri, "mms://", 6) == 0) {
		pd->uri = g_strdup_printf ("http://%s", uri + 6);
		pd->mmsh = true;
	} else
		pd->uri = g_strdup (uri);

}

static void
p_downloader_mmsh_reader (BrowserMmshResponse *response, gpointer context, char *buffer, int offset, PRUint32 length)
{
	StreamNotify *notify = (StreamNotify*) context;
	Downloader *dl = (Downloader *) notify->pdata;

	if (downloader_shutdown) {
		response->Abort ();
		return;
	}

	dl->Write (buffer, offset, length);

}

static void
p_downloader_mmsh_notifier (BrowserMmshResponse *response, gpointer context, char *name, int64_t size)
{
	StreamNotify *notify = (StreamNotify*) context;
	Downloader *dl = (Downloader *) notify->pdata;

	g_print ("Notifing a size of %d\n", size);
	dl->NotifySize (size);
}


static void
p_downloader_mmsh_finished (BrowserMmshResponse *response, gpointer context)
{
	char *fname;
	StreamNotify *notify = (StreamNotify*) context;
	Downloader *dl = (Downloader *) notify->pdata;

	if (downloader_shutdown) {
		return;
	}

	fname = dl->GetResponseFile (NULL);
        dl->NotifyFinished (fname);

}


static NPError
p_downloader_mmsh_send (NPP_t *plugin, const char *uri, StreamNotify *notify)
{
	BrowserMmshRequest *mmsh_request = new BrowserMmshRequest ("GET", uri);
	mmsh_request->SetHttpHeader ("User-Agent", "NSPlayer/11.1.0.3856");
	mmsh_request->SetHttpHeader ("Pragma", "no-cache,rate=1.000000,stream-time=0,stream-offset=0:0,max-duration=0");
	mmsh_request->SetHttpHeader ("Pragma", "xClientGUID={c77e7400-738a-11d2-9add-0020af0a3278}");
	mmsh_request->SetHttpHeader ("Pragma", "xPlayStrm=1");
	mmsh_request->SetHttpHeader ("Pragma", "stream-switch-count=0");
	mmsh_request->SetHttpHeader ("Pragma", "stream-switch-entry=ffff:1:0 ffff:4:0");
	return mmsh_request->GetAsyncResponse (p_downloader_mmsh_reader, p_downloader_mmsh_notifier, p_downloader_mmsh_finished, notify);
	
}


static void
p_downloader_send (gpointer state)
{
	PluginDownloader *pd = (PluginDownloader *) state;
	NPP_t *plugin = NULL;
	
	//fprintf (stderr, "PluginDownloaderSend: Starting downloader again for (%s %s)\n", pd->verb, pd->uri);
	
	if (pd && pd->dl && pd->dl->GetContext ()) {
		// Get the context from the downloader.
		plugin = ((PluginInstance *) pd->dl->GetContext ())->getInstance ();
	} else if (plugin_instances && plugin_instances->data) {
		// TODO: Review if we really should allowing download with the first plugin.
		plugin = (NPP_t *) plugin_instances->data;
		//printf ("DOWNLOADING WITH FIRST PLUGIN (%p), (Downloader->id: %i): %s\n", plugin, pd->dl->id, pd->uri);
	}
	
	if (plugin && pd) {
		StreamNotify *notify = new StreamNotify (StreamNotify::DOWNLOADER, pd->dl);
		Downloader *dl = (Downloader *) notify->pdata;
		NPError err;
		
		if (pd->mmsh) {
			err = p_downloader_mmsh_send (plugin, pd->uri, notify);
		} else {
			err = NPN_GetURLNotify (plugin, pd->uri, NULL, notify);
		}
		if (err != NPERR_NO_ERROR) {
			const char *msg;
			
			switch (err) {
			case NPERR_GENERIC_ERROR:
				msg = "generic error";
				break;
			case NPERR_OUT_OF_MEMORY_ERROR:
				msg = "out of memory";
				break;
			case NPERR_INVALID_URL:
				msg = "invalid url requested";
				break;
			case NPERR_FILE_NOT_FOUND:
				msg = "file not found";
				break;
			default:
				msg = "unknown error";
				break;
			}
			
			dl->NotifyFailed (msg);
		}
	}

	//
	// TODO:
	//
	//   Write the data to the stream, we should reconsider the idea of
	//   keeping everything in memory as whole documents and zip files
	//   can be transfered (see the XPS reader for silverlight):
	//              http://tinyurl.com/2n55mp
	//
	//
}

static void
p_downloader_abort (gpointer state)
{
	PluginDownloader *pd = (PluginDownloader *) state;
	
	if (downloader_shutdown)
		return;
	
	if (pd->npp && pd->stream) {
		NPN_DestroyStream (pd->npp, pd->stream, NPRES_USER_BREAK);
		pd->stream = NULL;
	}
}

void
downloader_set_stream_data (Downloader *downloader, NPP npp, NPStream *stream)
{
	PluginDownloader *pd = (PluginDownloader *) downloader->GetDownloaderState ();
	pd->npp = npp;
	pd->stream = stream;
	stream->pdata = pd;
}


void
downloader_initialize (void)
{
	downloader_shutdown = false;
	downloader_set_functions (
		p_downloader_create_state,
		p_downloader_destroy_state,
		p_downloader_open,
		p_downloader_send,
		p_downloader_abort);
}

void
downloader_destroy (void)
{
	downloader_shutdown = true;
}

