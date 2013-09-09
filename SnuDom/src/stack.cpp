#include "stack.h"
#include <string.h>

int
stack_grow(struct stack *st, size_t new_size)
{
	void **new_st;

	if (st->asize >= new_size)
		return 0;

	new_st = (void**)st->allocate(st->opaque, new_size * sizeof(void *));
	if (new_st == NULL)
		return -1;

	if(st->asize != 0)
		memcpy(new_st, st->item, new_size * sizeof(void *));

	memset(new_st + st->asize, 0x0,
		(new_size - st->asize) * sizeof(void *));

	st->item = new_st;
	st->asize = new_size;

	if (st->size > new_size)
		st->size = new_size;

	return 0;
}

void
stack_free(struct stack *st)
{
	if (!st)
		return;

	//free(st->item);

	st->item = NULL;
	st->size = 0;
	st->asize = 0;
}

int
stack_init(void* opaque, void* (*allocate)(void *opaque, size_t size), struct stack *st, size_t initial_size)
{
	st->item = NULL;
	st->size = 0;
	st->asize = 0;
	st->opaque = opaque;
	st->allocate = allocate;

	if (!initial_size)
		initial_size = 8;

	return stack_grow(st, initial_size);
}

void *
stack_pop(struct stack *st)
{
	if (!st->size)
		return NULL;

	return st->item[--st->size];
}

int
stack_push(struct stack *st, void *item)
{
	if (stack_grow(st, st->size * 2) < 0)
		return -1;

	st->item[st->size++] = item;
	return 0;
}

void *
stack_top(struct stack *st)
{
	if (!st->size)
		return NULL;

	return st->item[st->size - 1];
}

