﻿// Copyright 2008 Louis DeJardin - http://whereslou.com
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
namespace Castle.MonoRail.Views.Spark
{
    using Castle.MonoRail.Framework.Resources;

    public class SparkViewData
    {
        private readonly SparkView _view;

        public SparkViewData(SparkView view)
        {
            _view = view;
        }

        public object Eval(string key)
        {
            return PropertyBag(key) ??
                    Flash(key) ??
                    Helpers(key) ??
                    Params(key) ??
                    Resources(key);
        }

        public object this[string key]
        {
            get
            {
                return Eval(key);
            }
        }

        object PropertyBag(string key)
        {
            return _view.PropertyBag.Contains(key) ? _view.PropertyBag[key] : null;
        }
        object Flash(string key)
        {
            return _view.Flash.Contains(key) ? _view.Flash[key] : null;
        }
        object Helpers(string key)
        {
            return _view.ControllerContext.Helpers.Contains(key) ? _view.ControllerContext.Helpers[key] : null;
        }
        object Params(string key)
        {
            return _view.Params[key];
        }
        object Resources(string key)
        {
            IResource value;
            return _view.ControllerContext.Resources.TryGetValue(key, out value) ? value : null;
        }
    }
}
