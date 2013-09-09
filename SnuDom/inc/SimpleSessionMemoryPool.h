#pragma once

#include <memory>
#include <list>

class SimpleSessionMemoryPool
{
private:
	struct alloc_block
	{
	public:
		alloc_block(size_t allocatedSize) : currentPos(0) 
		{
			this->allocatedSize = allocatedSize;
			dataBlock = std::unique_ptr<uint8_t>(new uint8_t[allocatedSize]);
		}

		size_t currentPos;
		size_t allocatedSize;
		std::unique_ptr<uint8_t> dataBlock;

		void* alloc(size_t size)
		{
			auto result = dataBlock.get() + currentPos;
			currentPos += size;
			return result;
		}
	};
	std::list<alloc_block> allocBlocks;

	size_t smallestBlockSize(size_t targetSize)
	{
		if(targetSize < (64 * 1024))
			return (64 * 1024);
		else
		{
			auto multiple = targetSize / (64 * 1024);
			auto remainder = targetSize % (64 * 1024);
			if(remainder != 0)
				return (multiple + 1) * (64 * 1024);
			else
				return multiple * (64 * 1024);
		}
		
	}

public:
	void* alloc(size_t size)
	{
		if(allocBlocks.size() == 0)
		{
			allocBlocks.emplace_back(smallestBlockSize(size));
		}

		auto& initialTargetBlock = allocBlocks.back();
		if((initialTargetBlock.currentPos + size) >= initialTargetBlock.allocatedSize)
		{
			allocBlocks.emplace_back(smallestBlockSize(size));
		}

		return allocBlocks.back().alloc(size);
	}
};