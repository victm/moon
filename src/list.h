/*
 * list.h: a non-sucky linked list implementation
 *
 * Author: Jeffrey Stedfast <fejj@novell.com>
 *
 * Copyright 2007 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 */


#ifndef __LIST_H__
#define __LIST_H__


class List {
public:
	class Node {
	public:
		Node *next;
		Node *prev;
		
		// public
		Node ();
		virtual ~Node () { }
	};
	
	typedef bool (* NodeAction) (Node *node, void *data);
	
protected:
	Node *head;
	Node *tail;
	
public:
	// constructors
	List ();
	
	// properties
	Node *First ();
	Node *Last ();
	bool IsEmpty ();
	int Length ();
	
	// methods
	void Clear (bool freeNodes);
	
	Node *Append (Node *node);
	Node *Prepend (Node *node);
	Node *Insert (Node *node, int index);
	
	Node *Replace (Node *node, int index);
	
	Node *Find (NodeAction find, void *data);
	void Remove (NodeAction find, void *data);
	void Unlink (Node *node);
	
	Node *Index (int index);
	
	int IndexOf (Node *node);
	int IndexOf (NodeAction find, void *data);

	void ForEach (NodeAction action, void *data);
};

#endif /* __LIST_H__ */
