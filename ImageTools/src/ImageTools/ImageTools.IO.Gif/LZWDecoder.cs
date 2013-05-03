﻿// ===============================================================================
// LZWDecoder.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ImageTools.Helpers;

namespace ImageTools.IO.Gif
{
    /// <summary>
    /// Uncrompress data using the LZW algorithmus.
    /// </summary>
    sealed class LZWDecoder
    {
        private const int StackSize = 4096;
        private const int NullCode = -1;

        private Stream _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="LZWDecoder"/> class
        /// and sets the stream, where the compressed data should be read from.
        /// </summary>
        /// <param name="stream">The stream. where to read from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null
        /// (Nothing in Visual Basic).</exception>
        public LZWDecoder(Stream stream)
        {
            Guard.NotNull(stream, "stream");

            _stream = stream;
        }

        /// <summary>
        /// Decodes and uncompresses all pixel indices from the stream.
        /// </summary>
        /// <param name="width">The width of the pixel index array.</param>
        /// <param name="height">The height of the pixel index array.</param>
        /// <param name="dataSize">Size of the data.</param>
        /// <returns>The decoded and uncompressed array.</returns>
        public byte[] DecodePixels(int width, int height, int dataSize)
        {
            // The resulting index table.
            byte[] pixels = new byte[width * height];

            // Calculate the clear code. The value of the clear code is 2 ^ dataSize
            int clearCode = 1 << dataSize;

            if (dataSize == Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("dataSize", "Must be less than Int32.MaxValue");
            }

            int codeSize = dataSize + 1;

            // Calculate the end code
            int endCode = clearCode + 1;

            // Calculate the available code.
            int availableCode = clearCode + 2;

            #region Jillzhangs Code (Not From Me) see: http://giflib.codeplex.com/ 

            int code = NullCode; //ÓÃÓÚ´æ´¢µ±Ç°µÄ±àÂëÖµ
            int old_code = NullCode;//ÓÃÓÚ´æ´¢ÉÏÒ»´ÎµÄ±àÂëÖµ
            int code_mask = (1 << codeSize) - 1;//±íÊ¾±àÂëµÄ×î´óÖµ£¬Èç¹ûcodeSize=5,Ôòcode_mask=31
            int bits = 0;//ÔÚ±àÂëÁ÷ÖÐÊý¾ÝµÄ±£´æÐÎÊ½Îªbyte£¬¶øÊµ¼Ê±àÂë¹ý³ÌÖÐÊÇÕÒÊµ¼Ê±àÂëÎ»À´´æ´¢µÄ£¬±ÈÈçµ±codeSize=5µÄÊ±ºò£¬ÄÇÃ´Êµ¼ÊÉÏ5bitµÄÊý¾Ý¾ÍÓ¦¸Ã¿ÉÒÔ±íÊ¾Ò»¸ö±àÂë£¬ÕâÑùÈ¡³öÀ´µÄ1¸ö×Ö½Ú¾Í¸»ÓàÁË3¸öbit£¬Õâ3¸öbitÓÃÓÚºÍµÚ¶þ¸ö×Ö½ÚµÄºóÁ½¸öbit½øÐÐ×éºÏ£¬ÔÙ´ÎÐÎ³É±àÂëÖµ£¬Èç´ËÀàÍÆ


            int[] prefix = new int[StackSize];//ÓÃÓÚ±£´æÇ°×ºµÄ¼¯ºÏ
            int[] suffix = new int[StackSize];//ÓÃÓÚ±£´æºó×º
            int[] pixelStatck = new int[StackSize + 1];//ÓÃÓÚÁÙÊ±±£´æÊý¾ÝÁ÷

            int top = 0;
            int count = 0;//ÔÚÏÂÃæµÄÑ­»·ÖÐ£¬Ã¿´Î»á»ñÈ¡Ò»¶¨Á¿µÄ±àÂëµÄ×Ö½ÚÊý×é£¬¶ø´¦ÀíÕâÐ©Êý×éµÄÊ±ºòÐèÒª1¸ö¸ö×Ö½ÚÀ´´¦Àí£¬count¾ÍÊÇ±íÊ¾»¹Òª´¦ÀíµÄ×Ö½ÚÊýÄ¿
            int bi = 0;//count±íÊ¾»¹Ê£¶àÉÙ×Ö½ÚÐèÒª´¦Àí£¬¶øbiÔò±íÊ¾±¾´ÎÒÑ¾­´¦ÀíµÄ¸öÊý
            int xyz = 0;//i´ú±íµ±Ç°´¦ÀíµÃµ½ÏñËØÊý

            int data = 0;//±íÊ¾µ±Ç°´¦ÀíµÄÊý¾ÝµÄÖµ
            int first = 0;//Ò»¸ö×Ö·û´®ÖØµÄµÚÒ»¸ö×Ö½Ú
            int inCode = NullCode; //ÔÚlzwÖÐ£¬Èç¹ûÈÏÊ¶ÁËÒ»¸ö±àÂëËù´ú±íµÄÊý¾Ýentry£¬Ôò½«±àÂë×÷ÎªÏÂÒ»´ÎµÄprefix£¬´Ë´¦inCode´ú±í´«µÝ¸øÏÂÒ»´Î×÷ÎªÇ°×ºµÄ±àÂëÖµ

            //ÏÈÉú³ÉÔªÊý¾ÝµÄÇ°×º¼¯ºÏºÍºó×º¼¯ºÏ£¬ÔªÊý¾ÝµÄÇ°×º¾ùÎª0£¬¶øºó×ºÓëÔªÊý¾ÝÏàµÈ£¬Í¬Ê±±àÂëÒ²ÓëÔªÊý¾ÝÏàµÈ
			for (code = 0; code < StackSize; code++)
            {
                //Ç°×º³õÊ¼Îª0
                prefix[code] = 0;
                //ºó×º=ÔªÊý¾Ý=±àÂë
                suffix[code] = (byte)code;
            }

            byte[] buffer = null;
            while (xyz < pixels.Length)
            {
                //×î´óÏñËØÊýÒÑ¾­È·¶¨ÎªpixelCount = width * width
                if (top == 0)
                {
                    if (bits < codeSize)
                    {
                        //Èç¹ûµ±Ç°µÄÒª´¦ÀíµÄbitÊýÐ¡ÓÚ±àÂëÎ»´óÐ¡£¬ÔòÐèÒª¼ÓÔØÊý¾Ý
                        if (count == 0)
                        {
                            //Èç¹ûcountÎª0£¬±íÊ¾Òª´Ó±àÂëÁ÷ÖÐ¶ÁÒ»¸öÊý¾Ý¶ÎÀ´½øÐÐ·ÖÎö
                            buffer = ReadBlock();
                            count = buffer.Length;
                            if (count == 0)
                            {
                                //ÔÙ´ÎÏë¶ÁÈ¡Êý¾Ý¶Î£¬È´Ã»ÓÐ¶Áµ½Êý¾Ý£¬´ËÊ±¾Í±íÃ÷ÒÑ¾­´¦ÀíÍêÁË
                                break;
                            }
                            //ÖØÐÂ¶ÁÈ¡Ò»¸öÊý¾Ý¶Îºó£¬Ó¦¸Ã½«ÒÑ¾­´¦ÀíµÄ¸öÊýÖÃ0
                            bi = 0;
                        }
                        //»ñÈ¡±¾´ÎÒª´¦ÀíµÄÊý¾ÝµÄÖµ
                        data += buffer[bi] << bits;//´Ë´¦ÎªºÎÒªÒÆÎ»ÄØ£¬±ÈÈçµÚÒ»´Î´¦ÀíÁË1¸ö×Ö½ÚÎª176£¬µÚÒ»´ÎÖ»Òª´¦Àí5bit¾Í¹»ÁË£¬Ê£ÏÂ3bitÁô¸øÏÂ¸ö×Ö½Ú½øÐÐ×éºÏ¡£Ò²¾ÍÊÇµÚ¶þ¸ö×Ö½ÚµÄºóÁ½Î»+µÚÒ»¸ö×Ö½ÚµÄÇ°ÈýÎ»×é³ÉµÚ¶þ´ÎÊä³öÖµ
                        bits += 8;//±¾´ÎÓÖ´¦ÀíÁËÒ»¸ö×Ö½Ú£¬ËùÒÔÐèÒª+8                    
                        bi++;//½«´¦ÀíÏÂÒ»¸ö×Ö½Ú
                        count--;//ÒÑ¾­´¦Àí¹ýµÄ×Ö½ÚÊý+1
                        continue;
                    }
                    //Èç¹ûÒÑ¾­ÓÐ×ã¹»µÄbitÊý¿É¹©´¦Àí£¬ÏÂÃæ¾ÍÊÇ´¦Àí¹ý³Ì
                    //»ñÈ¡±àÂë
                    code = data & code_mask;//»ñÈ¡dataÊý¾ÝµÄ±àÂëÎ»´óÐ¡bitµÄÊý¾Ý
                    data >>= codeSize;//½«±àÂëÊý¾Ý½ØÈ¡ºó£¬Ô­À´µÄÊý¾Ý¾ÍÊ£ÏÂ¼¸¸öbitÁË£¬´ËÊ±½«ÕâÐ©bitÓÒÒÆ£¬ÎªÏÂ´Î×÷×¼±¸
                    bits -= codeSize;//Í¬Ê±ÐèÒª½«µ±Ç°Êý¾ÝµÄbitÊý¼õÈ¥±àÂëÎ»³¤£¬ÒòÎªÒÑ¾­µÃµ½ÁË´¦Àí¡£

                    //ÏÂÃæ¸ù¾Ý»ñÈ¡µÄcodeÖµÀ´½øÐÐ´¦Àí

                    if (code > availableCode || code == endCode)
                    {
                        //µ±±àÂëÖµ´óÓÚ×î´ó±àÂëÖµ»òÕßÎª½áÊø±ê¼ÇµÄÊ±ºò£¬Í£Ö¹´¦Àí                     
                        break;
                    }
                    if (code == clearCode)
                    {
                        //Èç¹ûµ±Ç°ÊÇÇå³ý±ê¼Ç£¬ÔòÖØÐÂ³õÊ¼»¯±äÁ¿£¬ºÃÖØÐÂÔÙÀ´
                        codeSize = dataSize + 1;
                        //ÖØÐÂ³õÊ¼»¯×î´ó±àÂëÖµ
                        code_mask = (1 << codeSize) - 1;
                        //³õÊ¼»¯ÏÂÒ»²½Ó¦¸Ã´¦ÀíµÃ±àÂëÖµ
                        availableCode = clearCode + 2;
                        //½«±£´æµ½old_codeÖÐµÄÖµÇå³ý£¬ÒÔ±ãÖØÍ·ÔÙÀ´
                        old_code = NullCode;
                        continue;
                    }
                    //ÏÂÃæÊÇcodeÊôÓÚÄÜÑ¹ËõµÄ±àÂë·¶Î§ÄÚµÄµÄ´¦Àí¹ý³Ì
                    if (old_code == NullCode)
                    {
                        //Èç¹ûµ±Ç°±àÂëÖµÎª¿Õ,±íÊ¾ÊÇµÚÒ»´Î»ñÈ¡±àÂë
                        pixelStatck[top++] = suffix[code];//»ñÈ¡µ½1¸öÊý¾ÝÁ÷µÄÊý¾Ý
                        //±¾´Î±àÂë´¦ÀíÍê³É£¬½«±àÂëÖµ±£´æµ½old_codeÖÐ
                        old_code = code;
                        //µÚÒ»¸ö×Ö·ûÎªµ±Ç°±àÂë
                        first = code;
                        continue;
                    }
                    inCode = code;
                    if (code == availableCode)
                    {
                        //Èç¹ûµ±Ç°±àÂëºÍ±¾´ÎÓ¦¸ÃÉú³ÉµÄ±àÂëÏàÍ¬
                        pixelStatck[top++] = (byte)first;//ÄÇÃ´ÏÂÒ»¸öÊý¾Ý×Ö½Ú¾ÍµÈÓÚµ±Ç°´¦Àí×Ö·û´®µÄµÚÒ»¸ö×Ö½Ú
                        code = old_code; //»ØËÝµ½ÉÏÒ»¸ö±àÂë
                    }
                    while (code > clearCode)
                    {
                        //Èç¹ûµ±Ç°±àÂë´óÓÚÇå³ý±ê¼Ç£¬±íÊ¾±àÂëÖµÊÇÄÜÑ¹ËõÊý¾ÝµÄ
                        pixelStatck[top++] = suffix[code];
                        code = prefix[code];//»ØËÝµ½ÉÏÒ»¸ö±àÂë
                    }
                    first = suffix[code];
                    if (availableCode >= StackSize)
                    {
                        //µ±±àÂë×î´óÖµ´óÓÚgifËùÔÊÐíµÄ±àÂë£¨4096£©×î´óÖµµÄÊ±ºòÍ£Ö¹´¦Àí
                        break;
                    }
                    //»ñÈ¡ÏÂÒ»¸öÊý¾Ý
                    pixelStatck[top++] = suffix[code];
                    //ÉèÖÃµ±Ç°Ó¦¸Ã±àÂëÎ»ÖÃµÄÇ°×º
                    prefix[availableCode] = old_code;
                    //ÉèÖÃµ±Ç°Ó¦¸Ã±àÂëÎ»ÖÃµÄºó×º
                    suffix[availableCode] = first;
                    //ÏÂ´ÎÓ¦¸ÃµÃµ½µÄ±àÂëÖµ
                    availableCode++;
                    if (availableCode == code_mask + 1 && availableCode < StackSize)
                    {
                        //Ôö¼Ó±àÂëÎ»Êý
                        codeSize++;
                        //ÖØÉè×î´ó±àÂëÖµ
                        code_mask = (1 << codeSize) - 1;
                    }
                    //»¹Ô­old_code
                    old_code = inCode;
                }
                //»ØËÝµ½ÉÏÒ»¸ö´¦ÀíÎ»ÖÃ
                top--;
                //»ñÈ¡ÔªÊý¾Ý              
                pixels[xyz++] = (byte)pixelStatck[top];
            }

            #endregion

            return pixels;
        }

        private byte[] ReadBlock()
        {   
            // Reads the next data block from the stream. A data block begins with a byte,
            // which defines the size of the block, followed by the block itself.
            int blockSize = _stream.ReadByte();

            return ReadBytes(blockSize);
        }

        private byte[] ReadBytes(int length)
        {
            // Reads the specified number of bytes from the data stream.
            byte[] buffer = new byte[length];

            _stream.Read(buffer, 0, length);

            return buffer;
        }
    }
}
