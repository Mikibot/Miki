﻿using Miki.Framework.Events.Attributes;
using Miki.Common;
using System.Threading.Tasks;
using Miki.Framework.Events;
using Miki.Framework.Extension;
using Miki.Discord.Common;
using Miki.Discord;

namespace Miki.Modules
{
    [Module(Name = "reactions")]
    public class ReactionsModule
    {
        [Command(Name = "confused")]
        public async Task ConfusedAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://i.imgur.com/RCotXAK.png",
                "http://i.imgur.com/yN5cwQq.jpg",
                "http://i.imgur.com/5TkmRWv.png",
                "http://i.imgur.com/QBFQzCQ.png",
                "http://i.imgur.com/KjSp1W4.png",
                "http://i.imgur.com/mX6D68m.jpg",
                "http://i.imgur.com/ogA5GeN.jpg",
                "http://i.imgur.com/eCHsHQZ.jpg",
                "http://i.imgur.com/r0u2dBx.jpg",
                "http://i.imgur.com/d8oMJUg.jpg",
                "http://i.imgur.com/dkV331J.jpg",
                "http://i.imgur.com/U9N4oGR.jpg",
                "http://i.imgur.com/GA0ZtvR.jpg",
                "http://i.imgur.com/NQ2e3Dq.gif",
                "http://i.imgur.com/5HTugJ6.jpg",
                "http://i.imgur.com/MJrBLij.png",
                "http://i.imgur.com/JgjCHPd.jpg",
                "http://i.imgur.com/KIDXXHw.gif",
                "http://i.imgur.com/Eu0Yyqq.jpg",
                "http://i.imgur.com/P5V369I.png",
                "http://i.imgur.com/DtVEGde.gif",
                "http://i.imgur.com/xxNH850.jpg",
                "http://i.imgur.com/gytATzW.jpg",
                "http://i.imgur.com/UrDJVC0.jpg",
                "http://i.imgur.com/3GkAnYo.png",
                "http://i.imgur.com/qTXPgyI.jpg",
                "http://i.imgur.com/GmIXuso.png",
                "http://i.imgur.com/UM8XpgR.gif",
                "http://i.imgur.com/GhoKM0u.gif",
		"http://i.imgur.com/ehskzgF.gif",
		"http://i.imgur.com/2biawgF.gif",
		"http://i.imgur.com/D2WXDbd.gif",
		"http://i.imgur.com/1ogeK3A.gif",
		"http://i.imgur.com/djNBrtj.jpg",
		"http://i.imgur.com/VyabzAv.jpg"
            };


			new EmbedBuilder()
			{
				ImageUrl = images[MikiRandom.Next(0, images.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}
	[Command(Name = "dab")]
        public async Task DabAsync(EventContext e)
        {
        	string[] images = new string[]
           	{
                "https://i.imgur.com/Cfe7bWI.gif",
                "https://i.imgur.com/2amGx1y.gif",
                "https://i.imgur.com/YeuLLNC.mp4",
		"https://i.imgur.com/vzwCDSt.mp4",
		"https://i.imgur.com/EyewECt.gif",
		"https://i.imgur.com/DsIMslz.gif",
		"https://i.imgur.com/nf8HRh2.gif"
           	};

			new EmbedBuilder()
			{
				ImageUrl = images[MikiRandom.Next(0, images.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}

	}
	
        [Command(Name = "lewd")]
        public async Task LewdAsync(EventContext e)
        {
            string[] lewd = new string[]
            {
                "http://i.imgur.com/eG42EVs.png",
                "http://i.imgur.com/8shK3jh.png",
                "http://i.imgur.com/uLKC84x.jpg",
                "http://i.imgur.com/PZCwyyE.png",
                "http://i.imgur.com/KWklw30.png",
                "http://i.imgur.com/aoLsNgx.jpg",
                "http://i.imgur.com/wyJAMVt.jpg",
                "http://i.imgur.com/2Y5ZgHH.png",
                "http://i.imgur.com/OIZyqxL.jpg",
                "http://i.imgur.com/cejd1c0.gif",
                "http://i.imgur.com/Obl7JvE.png",
                "http://i.imgur.com/PFFmM1q.png",
                "http://i.imgur.com/2vopeCM.jpg",
                "http://i.imgur.com/U4Nk0e5.jpg",
                "http://i.imgur.com/Llf61b1.jpg",
                "http://i.imgur.com/3vYPbuO.jpg",
                "http://i.imgur.com/p1twVD4.png",
                "http://i.imgur.com/kRaopT0.gif",
                "http://i.imgur.com/On8Axls.gif",
                "http://i.imgur.com/yCqJlFc.gif",
		"http://i.imgur.com/jlTqATG.gif"
            };


			new EmbedBuilder()
			{
				ImageUrl = lewd[MikiRandom.Next(0, lewd.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}

        [Command(Name = "pout")]
        public async Task PoutAsync(EventContext e)
        {
            string[] images = new string[]
            {
                "http://i.imgur.com/hsjBcz1.jpg",
                "http://i.imgur.com/oJSVNzT.jpg",
                "http://i.imgur.com/gWtmHoN.jpg",
                "http://i.imgur.com/VFG9zKV.png",
                "http://i.imgur.com/BUBiL0f.jpg",
                "http://i.imgur.com/UdlZ69E.gif",
                "http://i.imgur.com/yhryAf9.png",
                "http://i.imgur.com/d9DG2sJ.png",
                "http://i.imgur.com/uTB2HIY.png",
                "http://i.imgur.com/dVtR9vI.png",
                "http://i.imgur.com/rt7Vgn3.jpg",
                "http://i.imgur.com/uTB2HIY.png"
            };


			new EmbedBuilder()
			{
				ImageUrl = images[MikiRandom.Next(0, images.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command( Name = "smug" )]
		public async Task SmugAsync( EventContext e )
		{
			string[] images = new string[]
			{
				"http://i.imgur.com/zUwqrhM.png",
				"http://i.imgur.com/TYqPh89.jpg",
				"http://i.imgur.com/xyOSaCt.png",
				"http://i.imgur.com/gyw0ifl.png",
				"http://i.imgur.com/kk0xvtx.png",
				"http://i.imgur.com/UIuyUne.jpg",
				"http://i.imgur.com/9zgIjY1.jpg",
				"http://i.imgur.com/Ku1ONAD.jpg",
				"http://i.imgur.com/7lB5bRT.jpg",
				"http://i.imgur.com/BoVHipF.jpg",
				"http://i.imgur.com/vN48mwz.png",
				"http://i.imgur.com/fGI4zLe.jpg",
				"http://i.imgur.com/Gc4gmwQ.jpg",
				"http://i.imgur.com/JMrmKt7.jpg",
				"http://i.imgur.com/a7sbJz2.jpg",
				"http://i.imgur.com/NebmjhR.png",
				"http://i.imgur.com/5ccbrFI.png",
				"http://i.imgur.com/XJL4Vmo.jpg",
				"http://i.imgur.com/eg0q1ez.png",
				"http://i.imgur.com/JJFxxmA.jpg",
				"http://i.imgur.com/2cTDF3b.jpg",
				"http://i.imgur.com/Xc0Duqv.png",
				"http://i.imgur.com/YgMdPkd.jpg",
				"http://i.imgur.com/BvAv6an.jpg",
				"http://i.imgur.com/KRLP5JT.jpg",
				"http://i.imgur.com/yXcsCK3.jpg",
				"http://i.imgur.com/QXG56kG.png",
				"http://i.imgur.com/OFBz1YJ.png",
				"http://i.imgur.com/9ulVckY.png",
				"http://i.imgur.com/VLXeSJK.png",
				"http://i.imgur.com/baiMBP6.png"
			};

			new EmbedBuilder()
			{
				ImageUrl = images[MikiRandom.Next(0, images.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}

		[Command( Name = "cry" )]
		public async Task CryAsync( EventContext e )
		{
			string[] images = new string[]
			{
				"http://i.imgur.com/TTUBf2r.gif",
				"http://i.imgur.com/TP6dYGh.gif",
				"http://i.imgur.com/o66oQyX.png",
				"http://i.imgur.com/6AP78bD.png",
				"http://i.imgur.com/IvMvs2K.gif",
				"http://i.imgur.com/0kdQ38I.gif",
				"http://i.imgur.com/0kdQ38I.gif",
				"http://i.imgur.com/YHYLO4E.gif",
				"http://i.imgur.com/wXqxiDs.gif",
				"http://i.imgur.com/jzafqAh.gif",
				"http://i.imgur.com/2HPoWSf.gif",
				"http://i.imgur.com/W7prbbo.gif",
				"http://i.imgur.com/cKqKcG3.gif",
				"http://i.imgur.com/GKO0EQD.gif",
				"http://i.imgur.com/cu825ub.gif",
				"http://i.imgur.com/TP6dYGh.gif",
				"http://i.imgur.com/uZ2WXyL.gif",
				"http://i.imgur.com/DhkvnpB.gif",
				"http://i.imgur.com/LbpaJMG.gif",
				"http://i.imgur.com/V7iS3ZR.gif",
				"http://i.imgur.com/TLoHpfE.gif",
				"http://i.imgur.com/35tYOoB.gif",
				"http://i.imgur.com/Q6I2fiy.gif",
				"http://i.imgur.com/7Tw9dPP.gif",
				"http://i.imgur.com/aIiuJg8.gif",
				"http://i.imgur.com/0xIG1kG.gif",
				"http://i.imgur.com/nE0Tdp0.gif",
				"http://i.imgur.com/mvyAx5q.gif",
				"http://i.imgur.com/diq8LxU.gif",
				"http://i.imgur.com/Zv7au0h.gif",
				"http://i.imgur.com/sOyqImI.gif",
				"http://i.imgur.com/ZRbHJcb.gif",
				"http://i.imgur.com/kysvK28.gif",
				"http://i.imgur.com/6tGAJ75.gif",
				"http://i.imgur.com/5k6aD7Z.gif",
				"http://i.imgur.com/B29MytB.gif",
				"http://i.imgur.com/FQx8zRj.gif",
				"http://i.imgur.com/5vUBsz4.gif",
				"http://i.imgur.com/rBMTG5o.gif",
				"http://i.imgur.com/qfcReCj.gif",
				"http://i.imgur.com/CRdCCoH.gif",
				"http://i.imgur.com/FVt8Jqr.gif",
				"http://i.imgur.com/mjziZGI.gif",
				"http://i.imgur.com/DEgkwBe.gif",
				"http://i.imgur.com/hfRw1my.gif",
				"http://i.imgur.com/Sus5vcM.gif",
				"http://i.imgur.com/HLmnS6S.gif",
				"http://i.imgur.com/w9UjKVR.gif",
				"http://i.imgur.com/QZvnKHs.gif",
				"http://i.imgur.com/Mw49bFm.gif",
				"http://i.imgur.com/UVxws3C.gif",
				"http://i.imgur.com/ekhYSVB.gif",
				"http://i.imgur.com/VOMpsf6.gif",
				"http://i.imgur.com/ZFnoy0i.gif",
				"http://i.imgur.com/180yuVH.gif",
				"http://i.imgur.com/3zVAY49.gif",
				"http://i.imgur.com/AFDevRo.gif",
				"http://i.imgur.com/T23nHVO.gif",
				"http://i.imgur.com/qZWhIOw.gif",
				"http://i.imgur.com/Iy2VjHw.gif",
				"http://i.imgur.com/DbUYdpj.gif",
				"http://i.imgur.com/XqYZOiv.gif",
				"http://i.imgur.com/sYV2GBp.gif",
				"http://i.imgur.com/hxbNeGL.gif",
				"http://i.imgur.com/RXdJczP.gif",
				"http://i.imgur.com/JzmQgZq.gif",
				"http://i.imgur.com/NkLgdj8.gif",
				"http://i.imgur.com/kMzX2d4.gif",
				"http://i.imgur.com/WLNfW3d.gif",
				"http://i.imgur.com/Oxk8HUp.gif",
				"http://i.imgur.com/HTlRErM.gif",
				"http://i.imgur.com/KKgROec.gif",
				"http://i.imgur.com/W0WetV3.gif",
				"http://i.imgur.com/Ny9alj7.gif",
				"http://i.imgur.com/HNBYRZb.gif",
				"http://i.imgur.com/WOqFHee.gif",
				"http://i.imgur.com/rmlZXaP.gif",
				"http://i.imgur.com/mcVLAXi.gif",
				"http://i.imgur.com/SalWtcC.gif",
				"http://i.imgur.com/pkT7JFw.gif",
				"http://i.imgur.com/Tx15hPX.gif",
				"http://i.imgur.com/YANiZ2a.gif",
				"http://i.imgur.com/31WnXZ7.gif"
			};

			new EmbedBuilder()
			{
				ImageUrl = images[MikiRandom.Next(0, images.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}
	        
		[Command(Name = "stare")]
        public async Task StareAsync(EventContext e)
        {
        	string[] images = new string[]
           	{
                "http://i.imgur.com/Bp9vfcf.gif",
                "http://i.imgur.com/7nFd5ve.gif",
                "http://i.imgur.com/rmfWuM0.gif"
           	};

			new EmbedBuilder()
			{
				ImageUrl = images[MikiRandom.Next(0, images.Length)]
			}.ToEmbed().QueueToChannel(e.Channel);
		}

	}
}
